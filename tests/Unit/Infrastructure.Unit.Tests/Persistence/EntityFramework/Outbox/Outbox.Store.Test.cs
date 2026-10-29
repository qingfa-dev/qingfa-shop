using Application.Common.Messaging.Outbox;

using Core.Common.Monad.Results;

using Infrastructure.Persistence.EntityFramework.Outbox;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Outbox;

/// <summary>Unit tests for the EF Core outbox store's tracked-entity operations.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Messaging")]
public sealed class OutboxStoreTest
{
    [Fact(DisplayName = "Enqueue stages rows without saving")]
    public async Task EnqueueAsync_WithMessages_ShouldStageRowsWithoutSaving()
    {
        // Arrange
        string databaseName = NewDatabaseName();
        await using OutboxTestContext context = CreateContext(databaseName);
        var store = new EfCoreOutboxStore(context);
        OutboxMessage first = NewMessage();
        OutboxMessage second = NewMessage();

        // Act
        Result result = await store.EnqueueAsync([first, second]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Entries<OutboxMessage>().Should().HaveCount(2)
            .And.OnlyContain(entry => entry.State == EntityState.Added);

        // Stage only: a second context must not see the rows before a save.
        await using OutboxTestContext reader = CreateContext(databaseName);
        (await reader.Set<OutboxMessage>().CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "Enqueue with a null collection throws")]
    public async Task EnqueueAsync_WithNullMessages_ShouldThrow()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);

        // Act
        Func<Task> act = () => store.EnqueueAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Mark processed sets the timestamp and clears the lease")]
    public async Task MarkProcessedAsync_WithLiveLease_ShouldProcessAndClearLease()
    {
        // Arrange
        string databaseName = NewDatabaseName();
        await using OutboxTestContext context = CreateContext(databaseName);
        var store = new EfCoreOutboxStore(context);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 2);
        context.Add(message);
        await context.SaveChangesAsync();
        DateTimeOffset processedOnUtc = DateTimeOffset.UtcNow;

        // Act
        Result result = await store.MarkProcessedAsync(message.Id, leaseId, processedOnUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(databaseName, message.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Processed);
        stored.ProcessedOnUtc.Should().Be(processedOnUtc);
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
        stored.LastError.Should().BeNull();
    }

    [Fact(DisplayName = "Mark failed increments attempts and reschedules")]
    public async Task MarkFailedAsync_WithLiveLease_ShouldIncrementAttemptsAndReschedule()
    {
        // Arrange
        string databaseName = NewDatabaseName();
        await using OutboxTestContext context = CreateContext(databaseName);
        var store = new EfCoreOutboxStore(context);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 2);
        context.Add(message);
        await context.SaveChangesAsync();
        DateTimeOffset nextAttemptUtc = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        Result result = await store.MarkFailedAsync(
            message.Id, leaseId, "broker unavailable", nextAttemptUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(databaseName, message.Id);
        stored.Status.Should().Be(OutboxMessageStatus.Pending);
        stored.Attempts.Should().Be(3);
        stored.LastError.Should().Be("broker unavailable");
        stored.AvailableOnUtc.Should().Be(nextAttemptUtc);
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Dead letter moves the message to the terminal state")]
    public async Task DeadLetterAsync_WithLiveLease_ShouldSetDeadLettered()
    {
        // Arrange
        string databaseName = NewDatabaseName();
        await using OutboxTestContext context = CreateContext(databaseName);
        var store = new EfCoreOutboxStore(context);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 9);
        context.Add(message);
        await context.SaveChangesAsync();

        // Act
        Result result = await store.DeadLetterAsync(message.Id, leaseId, "max attempts reached");

        // Assert
        result.IsSuccess.Should().BeTrue();
        OutboxMessage stored = await ReadAsync(databaseName, message.Id);
        stored.Status.Should().Be(OutboxMessageStatus.DeadLettered);
        stored.Attempts.Should().Be(10);
        stored.LastError.Should().Be("max attempts reached");
        stored.LeaseId.Should().BeNull();
        stored.LockedUntilUtc.Should().BeNull();
    }

    [Fact(DisplayName = "Count pending ignores processed and dead-lettered rows")]
    public async Task CountPendingAsync_ShouldCountOnlyPending()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        context.Add(NewMessage());
        context.Add(NewMessage());
        context.Add(NewMessage(OutboxMessageStatus.Processed));
        context.Add(NewMessage(OutboxMessageStatus.DeadLettered));
        await context.SaveChangesAsync();

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
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        DateTimeOffset oldest = DateTimeOffset.UtcNow.AddMinutes(-10);
        context.Add(NewMessage(occurredOnUtc: oldest));
        // A processed row is older still; it must not drive the lag metric.
        context.Add(NewMessage(OutboxMessageStatus.Processed, DateTimeOffset.UtcNow.AddMinutes(-20)));
        await context.SaveChangesAsync();

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
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);

        // Act
        Result result = await store.MarkProcessedAsync(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.NotFound);
    }

    [Fact(DisplayName = "A stale lease cannot mark the message")]
    public async Task MarkProcessedAsync_WithStaleLease_ShouldReturnLeaseLost()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        OutboxMessage message = NewClaimedMessage(Guid.NewGuid(), attempts: 1);
        context.Add(message);
        await context.SaveChangesAsync();

        // Act
        Result result = await store.MarkProcessedAsync(
            message.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.LeaseLost);
    }

    private static string NewDatabaseName() => Guid.NewGuid().ToString();

    [Theory(DisplayName = "Claim rejects a non-positive batch size")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ClaimAsync_WithNonPositiveBatchSize_ShouldThrow(int batchSize)
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);

        // Act
        Func<Task> act = () => store.ClaimAsync(
            batchSize, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Claim rejects a non-positive lease duration")]
    public async Task ClaimAsync_WithNonPositiveLease_ShouldThrow()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);

        // Act
        Func<Task> act = () => store.ClaimAsync(
            10, DateTimeOffset.UtcNow, TimeSpan.Zero);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Claim reports a provider that cannot transact")]
    public async Task ClaimAsync_WhenTheProviderCannotTransact_ShouldReturnClaimFailed()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);

        // Act
        Result<IReadOnlyList<OutboxMessage>> result = await store.ClaimAsync(
            10, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.ClaimFailed);
    }

    [Fact(DisplayName = "Count pending reports a disposed context")]
    public async Task CountPendingAsync_WhenTheContextIsDisposed_ShouldReturnFailure()
    {
        // Arrange
        OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        await context.DisposeAsync();

        // Act
        Result<int> result = await store.CountPendingAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Oldest pending reports a disposed context")]
    public async Task OldestPendingOnUtcAsync_WhenTheContextIsDisposed_ShouldReturnFailure()
    {
        // Arrange
        OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        await context.DisposeAsync();

        // Act
        Result<DateTimeOffset?> result = await store.OldestPendingOnUtcAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Retention deletes only processed rows on a relational provider")]
    public async Task DeleteProcessedBeforeAsync_WithSqlite_ShouldDeleteOnlyProcessedRows()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteOutboxTestContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SqliteOutboxTestContext(options);
        await context.Database.EnsureCreatedAsync();
        var store = new EfCoreOutboxStore(context);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        context.Add(NewMessage(
            OutboxMessageStatus.Processed,
            processedOnUtc: now.AddDays(-10)));
        context.Add(NewMessage(OutboxMessageStatus.Processed, processedOnUtc: now));
        context.Add(NewMessage());
        await context.SaveChangesAsync();

        // Act
        Result<int> result = await store.DeleteProcessedBeforeAsync(now.AddDays(-1));

        // Assert
        result.IsSuccess.Should().BeTrue(
            "the store should delete: {0}",
            string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}")));
        result.Value.Should().Be(1);
        (await context.Set<OutboxMessage>().CountAsync()).Should().Be(2);
    }

    [Fact(DisplayName = "Retention reports a disposed context")]
    public async Task DeleteProcessedBeforeAsync_WhenTheContextIsDisposed_ShouldReturnFailure()
    {
        // Arrange
        OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        await context.DisposeAsync();

        // Act
        Result<int> result = await store.DeleteProcessedBeforeAsync(DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "A lease changed after the load is reported as lost")]
    public async Task MarkProcessedAsync_WhenTheLeaseChangedSinceLoad_ShouldReturnLeaseLost()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
        Guid leaseId = Guid.NewGuid();
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 1);
        context.Add(message);
        await context.SaveChangesAsync();

        // Simulate: another worker re-claimed the row after this context loaded it.
        context.Entry(message).Property(candidate => candidate.LeaseId).OriginalValue =
            Guid.NewGuid();

        // Act
        Result result = await store.MarkProcessedAsync(
            message.Id, leaseId, DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.LeaseLost);
    }

    [Fact(DisplayName = "A save failure on mark processed is translated")]
    public async Task MarkProcessedAsync_WhenTheSaveFails_ShouldReturnMarkFailed()
    {
        // Arrange
        Guid leaseId = Guid.NewGuid();
        (string databaseName, Guid id) = await SeedClaimedAsync(leaseId);
        await using OutboxTestContext context = CreateThrowingContext(databaseName);
        var store = new EfCoreOutboxStore(context);

        // Act
        Result result = await store.MarkProcessedAsync(id, leaseId, DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.MarkFailed);
    }

    [Fact(DisplayName = "A save failure on mark failed is translated")]
    public async Task MarkFailedAsync_WhenTheSaveFails_ShouldReturnMarkFailed()
    {
        // Arrange
        Guid leaseId = Guid.NewGuid();
        (string databaseName, Guid id) = await SeedClaimedAsync(leaseId);
        await using OutboxTestContext context = CreateThrowingContext(databaseName);
        var store = new EfCoreOutboxStore(context);

        // Act
        Result result = await store.MarkFailedAsync(
            id, leaseId, "save failed", DateTimeOffset.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.MarkFailed);
    }

    [Fact(DisplayName = "A save failure on dead letter is translated")]
    public async Task DeadLetterAsync_WhenTheSaveFails_ShouldReturnDeadLetterFailed()
    {
        // Arrange
        Guid leaseId = Guid.NewGuid();
        (string databaseName, Guid id) = await SeedClaimedAsync(leaseId);
        await using OutboxTestContext context = CreateThrowingContext(databaseName);
        var store = new EfCoreOutboxStore(context);

        // Act
        Result result = await store.DeadLetterAsync(id, leaseId, "save failed");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(OutboxOutcome.Codes.DeadLetterFailed);
    }

    [Fact(DisplayName = "A failing message collection is translated")]
    public async Task EnqueueAsync_WhenTheCollectionFails_ShouldReturnEnqueueFailed()
    {
        // Arrange
        await using OutboxTestContext context = CreateContext(NewDatabaseName());
        var store = new EfCoreOutboxStore(context);
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

    private static async Task<(string DatabaseName, Guid Id)> SeedClaimedAsync(Guid leaseId)
    {
        string databaseName = NewDatabaseName();
        await using OutboxTestContext context = CreateContext(databaseName);
        OutboxMessage message = NewClaimedMessage(leaseId, attempts: 1);
        context.Add(message);
        await context.SaveChangesAsync();

        return (databaseName, message.Id);
    }

    private static OutboxTestContext CreateThrowingContext(string databaseName)
    {
        var builder = new DbContextOptionsBuilder<OutboxTestContext>();
        builder.UseInMemoryDatabase(databaseName);
        builder.AddInterceptors(new ThrowingSaveInterceptor());

        return new OutboxTestContext(builder.Options);
    }

    private static OutboxTestContext CreateContext(string databaseName)
        => new(new DbContextOptionsBuilder<OutboxTestContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);

    private static async Task<OutboxMessage> ReadAsync(string databaseName, Guid id)
    {
        await using OutboxTestContext reader = CreateContext(databaseName);
        return await reader.Set<OutboxMessage>().SingleAsync(message => message.Id == id);
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
            OccurredOnUtc = occurredOnUtc ?? DateTimeOffset.UtcNow,
            AvailableOnUtc = occurredOnUtc ?? DateTimeOffset.UtcNow,
            ProcessedOnUtc = processedOnUtc,
        };

    private static OutboxMessage NewClaimedMessage(Guid leaseId, int attempts) => new()
    {
        Type = "TodoCreatedEvent",
        Payload = "{}",
        Metadata = "{}",
        Attempts = attempts,
        LastError = "previous failure",
        LeaseId = leaseId,
        LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(5),
    };

    // Test-only DbContext: applies just the outbox mapping so the suite stays focused.
    private sealed class OutboxTestContext(DbContextOptions<OutboxTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        }
    }

    // Test-only SQLite context: converts the retention timestamp so ExecuteDelete stays translatable.
    private sealed class SqliteOutboxTestContext(DbContextOptions<SqliteOutboxTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

            modelBuilder.Entity<OutboxMessage>()
                .Property(message => message.ProcessedOnUtc)
                .HasConversion(new DateTimeOffsetToBinaryConverter());
        }
    }

    // Test-only interceptor: fails the save so the store's translation path is exercised.
    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
            => throw new InvalidOperationException("save failed");

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("save failed");
    }
}