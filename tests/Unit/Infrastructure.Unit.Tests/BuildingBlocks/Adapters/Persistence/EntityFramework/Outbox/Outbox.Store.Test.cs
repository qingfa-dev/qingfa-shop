using Application.BuildingBlocks.Messaging.Outbox;

using Core.BuildingBlocks.Functional.Results;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Unit tests for the EF Core outbox store's set-based operations.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Messaging")]
public sealed class OutboxStoreTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Enqueue stages rows without saving")]
    public async Task EnqueueAsync_WithMessages_ShouldStageRowsWithoutSaving()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        OutboxMessage first = NewMessage();
        OutboxMessage second = NewMessage();

        // Act
        Result result = await store.EnqueueAsync([first, second]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        database.Context.ChangeTracker.Entries<OutboxMessage>().Should().HaveCount(2)
            .And.OnlyContain(entry => entry.State == EntityState.Added);

        // Enqueue: a query must not see the rows before the owning unit of work saves.
        database.Context.ChangeTracker.Clear();
        (await database.Context.Set<OutboxMessage>().CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "Enqueue with a null collection throws")]
    public async Task EnqueueAsync_WithNullMessages_ShouldThrow()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);

        // Act
        Func<Task> act = () => store.EnqueueAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Enqueue translates a failing collection")]
    public async Task EnqueueAsync_WhenTheCollectionFails_ShouldReturnEnqueueFailed()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        var messages = new Mock<IReadOnlyCollection<OutboxMessage>>();
        messages.SetupGet(collection => collection.Count).Returns(1);
        messages.Setup(collection => collection.GetEnumerator())
            .Throws(new InvalidOperationException("boom"));

        // Act
        Result result = await store.EnqueueAsync(messages.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.EnqueueFailed);
    }

    [Fact(DisplayName = "Mark processed sets the timestamp and clears the lease")]
    public async Task MarkProcessedAsync_WithLiveLease_ShouldProcessAndClearLease()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 2);
        database.Context.Add(message);
        await database.Context.SaveChangesAsync();
        DateTimeOffset processedOnUtc = Now.AddSeconds(5);

        // Act
        Result result = await store.MarkProcessedAsync(message.Id, leaseId, processedOnUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(database.Context, message.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Processed);
        stored.ProcessedOnUtc.Should().Be(processedOnUtc);
        stored.Attempts.Should().Be(2);
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
        stored.LastError.Should().BeNull();
    }

    [Fact(DisplayName = "Mark failed increments attempts and reschedules")]
    public async Task MarkFailedAsync_WithLiveLease_ShouldIncrementAttemptsAndReschedule()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 2);
        database.Context.Add(message);
        await database.Context.SaveChangesAsync();
        DateTimeOffset nextAttemptUtc = Now.AddMinutes(5);

        // Act
        Result result = await store.MarkFailedAsync(
            message.Id, leaseId, "broker unavailable", nextAttemptUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(database.Context, message.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Pending);
        stored.Attempts.Should().Be(3);
        stored.LastError.Should().Be("broker unavailable");
        stored.AvailableOnUtc.Should().Be(nextAttemptUtc);
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Dead letter moves the message to the terminal state without touching attempts")]
    public async Task DeadLetterAsync_WithLiveLease_ShouldSetDeadLettered()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 9);
        database.Context.Add(message);
        await database.Context.SaveChangesAsync();

        // Act
        Result result = await store.DeadLetterAsync(message.Id, leaseId, "max attempts reached");

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(database.Context, message.Id);
        stored.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        stored.Attempts.Should().Be(9);
        stored.LastError.Should().Be("max attempts reached");
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Count pending ignores processed and dead-lettered rows")]
    public async Task CountPendingAsync_ShouldCountOnlyPending()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        database.Context.Add(NewMessage());
        database.Context.Add(NewMessage());
        database.Context.Add(NewMessage(OutboxMessageStatus.Processed));
        database.Context.Add(NewMessage(OutboxMessageStatus.DeadLettered));
        await database.Context.SaveChangesAsync();

        // Act
        Result<int> result = await store.CountPendingAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact(DisplayName = "Oldest pending reports the earliest pending occurrence")]
    public async Task OldestPendingOnUtcAsync_ShouldReturnEarliestPending()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        DateTimeOffset oldest = Now.AddMinutes(-10);
        database.Context.Add(NewMessage(occurredOnUtc: oldest));
        // Filter: a processed row is older still but must not drive the lag metric.
        database.Context.Add(NewMessage(
            OutboxMessageStatus.Processed,
            Now.AddMinutes(-20)));
        await database.Context.SaveChangesAsync();

        // Act
        Result<DateTimeOffset?> result = await store.OldestPendingOnUtcAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(oldest);
    }

    [Fact(DisplayName = "Unknown message returns not found")]
    public async Task MarkProcessedAsync_WithUnknownId_ShouldReturnNotFound()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);

        // Act
        Result result = await store.MarkProcessedAsync(
            Guid.NewGuid(), Guid.NewGuid(), Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.NotFound);
    }

    [Fact(DisplayName = "A stale lease cannot mark the message")]
    public async Task MarkProcessedAsync_WithStaleLease_ShouldReturnLeaseLost()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        OutboxMessage message = NewClaimedMessage(Guid.NewGuid(), attempts: 1);
        database.Context.Add(message);
        await database.Context.SaveChangesAsync();

        // Act
        Result result = await store.MarkProcessedAsync(message.Id, Guid.NewGuid(), Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.LeaseLost);
    }

    [Fact(DisplayName = "An expired lease cannot mark the message")]
    public async Task MarkProcessedAsync_WithExpiredLease_ShouldReturnLeaseLost()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(
            leaseId,
            attempts: 1,
            lockedUntilUtc: Now.AddMinutes(-1));
        database.Context.Add(message);
        await database.Context.SaveChangesAsync();

        // Act
        Result result = await store.MarkProcessedAsync(message.Id, leaseId, Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.LeaseLost);
    }

    [Fact(DisplayName = "Claim rejects a non-positive batch size")]
    public async Task ClaimAsync_WithNonPositiveBatchSize_ShouldThrow()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);

        // Act
        Func<Task> act = () => store.ClaimAsync(0, Now, TimeSpan.FromMinutes(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Claim rejects a non-positive lease duration")]
    public async Task ClaimAsync_WithNonPositiveLease_ShouldThrow()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);

        // Act
        Func<Task> act = () => store.ClaimAsync(10, Now, TimeSpan.Zero);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Claim translates a provider that cannot run the claim SQL")]
    public async Task ClaimAsync_WhenTheProviderDoesNotSupportTheSql_ShouldReturnClaimFailed()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);

        // Act
        Result<IReadOnlyList<OutboxMessage>> result = await store.ClaimAsync(
            10, Now, TimeSpan.FromMinutes(1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.ClaimFailed);
    }

    [Fact(DisplayName = "Mark processed reports a disposed context")]
    public async Task MarkProcessedAsync_WhenTheContextIsDisposed_ShouldReturnMarkFailed()
    {
        // Arrange
        OutboxTestContext context = CreateDisposedContext();
        var store = CreateStore(context, Now);

        // Act
        Result result = await store.MarkProcessedAsync(
            Guid.NewGuid(), Guid.NewGuid(), Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.MarkFailed);
    }

    [Fact(DisplayName = "Mark failed reports a disposed context")]
    public async Task MarkFailedAsync_WhenTheContextIsDisposed_ShouldReturnMarkFailed()
    {
        // Arrange
        OutboxTestContext context = CreateDisposedContext();
        var store = CreateStore(context, Now);

        // Act
        Result result = await store.MarkFailedAsync(
            Guid.NewGuid(), Guid.NewGuid(), "save failed", Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.MarkFailed);
    }

    [Fact(DisplayName = "Dead letter reports a disposed context")]
    public async Task DeadLetterAsync_WhenTheContextIsDisposed_ShouldReturnDeadLetterFailed()
    {
        // Arrange
        OutboxTestContext context = CreateDisposedContext();
        var store = CreateStore(context, Now);

        // Act
        Result result = await store.DeadLetterAsync(
            Guid.NewGuid(), Guid.NewGuid(), "save failed");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.DeadLetterFailed);
    }

    [Fact(DisplayName = "Count pending reports a disposed context")]
    public async Task CountPendingAsync_WhenTheContextIsDisposed_ShouldReturnQueryFailed()
    {
        // Arrange
        OutboxTestContext context = CreateDisposedContext();
        var store = CreateStore(context, Now);

        // Act
        Result<int> result = await store.CountPendingAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.QueryFailed);
    }

    [Fact(DisplayName = "Oldest pending reports a disposed context")]
    public async Task OldestPendingOnUtcAsync_WhenTheContextIsDisposed_ShouldReturnQueryFailed()
    {
        // Arrange
        OutboxTestContext context = CreateDisposedContext();
        var store = CreateStore(context, Now);

        // Act
        Result<DateTimeOffset?> result = await store.OldestPendingOnUtcAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.QueryFailed);
    }

    [Fact(DisplayName = "Retention deletes only processed rows")]
    public async Task DeleteProcessedBeforeAsync_ShouldDeleteOnlyProcessedRows()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        database.Context.Add(NewMessage(
            OutboxMessageStatus.Processed,
            processedOnUtc: Now.AddDays(-10)));
        database.Context.Add(NewMessage(OutboxMessageStatus.Processed, processedOnUtc: Now));
        database.Context.Add(NewMessage());
        await database.Context.SaveChangesAsync();

        // Act
        Result<int> result = await store.DeleteProcessedBeforeAsync(Now.AddDays(-1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        (await database.Context.Set<OutboxMessage>().CountAsync()).Should().Be(2);
    }

    [Fact(DisplayName = "Retention reports a disposed context")]
    public async Task DeleteProcessedBeforeAsync_WhenTheContextIsDisposed_ShouldReturnRetentionFailed()
    {
        // Arrange
        OutboxTestContext context = CreateDisposedContext();
        var store = CreateStore(context, Now);

        // Act
        Result<int> result = await store.DeleteProcessedBeforeAsync(Now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.RetentionFailed);
    }

    [Fact(DisplayName = "Claim rethrows cancellation")]
    public async Task ClaimAsync_WithCancelledToken_ShouldRethrow()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> act = () => store.ClaimAsync(
            10, Now, TimeSpan.FromMinutes(1), cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "Mark rethrows cancellation")]
    public async Task MarkProcessedAsync_WithCancelledToken_ShouldRethrow()
    {
        // Arrange
        await using SqliteOutbox database = await SqliteOutbox.CreateAsync();
        var store = CreateStore(database.Context, Now);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> act = () => store.MarkProcessedAsync(
            Guid.NewGuid(), Guid.NewGuid(), Now, cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static EfCoreOutboxStore CreateStore(
        OutboxTestContext context,
        DateTimeOffset utcNow)
        => new(context, new FixedTimeProvider(utcNow));

    private static OutboxTestContext CreateDisposedContext()
    {
        var context = new OutboxTestContext(
            new DbContextOptionsBuilder<OutboxTestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        context.Dispose();

        return context;
    }

    private static async Task<OutboxMessage> ReadAsync(OutboxTestContext context, Guid id)
    {
        context.ChangeTracker.Clear();

        return await context.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(message => message.Id == id);
    }

    private static OutboxMessage NewMessage(
        OutboxMessageStatus status = OutboxMessageStatus.Pending,
        DateTimeOffset? occurredOnUtc = null,
        DateTimeOffset? processedOnUtc = null) => new()
        {
            Type = "TodoCreatedEvent",
            Payload = "{}",
            Metadata = "{}",
            Status = status,
            OccurredOnUtc = occurredOnUtc ?? Now,
            AvailableOnUtc = occurredOnUtc ?? Now,
            ProcessedOnUtc = processedOnUtc,
        };

    private static OutboxMessage NewClaimedMessage(
        Guid leaseId,
        int attempts,
        DateTimeOffset? lockedUntilUtc = null) => new()
        {
            Type = "TodoCreatedEvent",
            Payload = "{}",
            Metadata = "{}",
            Attempts = attempts,
            LastError = "previous failure",
            LeaseId = leaseId,
            LockedUntilUtc = lockedUntilUtc ?? Now.AddMinutes(5),
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    // Test-only DbContext: SQLite so set-based updates and deletes execute for real.
    private sealed class OutboxTestContext(DbContextOptions<OutboxTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

            // Transform: SQLite cannot compare DateTimeOffset columns in set-based SQL.
            modelBuilder.Entity<OutboxMessage>()
                .Property(message => message.OccurredOnUtc)
                .HasConversion(new DateTimeOffsetToBinaryConverter());
            modelBuilder.Entity<OutboxMessage>()
                .Property(message => message.AvailableOnUtc)
                .HasConversion(new DateTimeOffsetToBinaryConverter());
            modelBuilder.Entity<OutboxMessage>()
                .Property(message => message.LockedUntilUtc)
                .HasConversion(new DateTimeOffsetToBinaryConverter());
            modelBuilder.Entity<OutboxMessage>()
                .Property(message => message.ProcessedOnUtc)
                .HasConversion(new DateTimeOffsetToBinaryConverter());
        }
    }

    // Owns the in-memory SQLite connection and the context that uses it.
    private sealed class SqliteOutbox : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteOutbox(SqliteConnection connection, OutboxTestContext context)
        {
            _connection = connection;
            Context = context;
        }

        public OutboxTestContext Context { get; }

        public static async Task<SqliteOutbox> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<OutboxTestContext>()
                .UseSqlite(connection)
                .Options;
            var context = new OutboxTestContext(options);
            await context.Database.EnsureCreatedAsync();

            return new SqliteOutbox(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}