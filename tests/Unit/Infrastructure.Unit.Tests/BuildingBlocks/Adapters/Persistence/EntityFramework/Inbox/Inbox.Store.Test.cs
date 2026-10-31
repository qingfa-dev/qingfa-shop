using System.Data;

using Application.BuildingBlocks.Messaging.Inbox;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Core.BuildingBlocks.Functional.Errors;
using Core.BuildingBlocks.Functional.Results;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Inbox;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Inbox;

/// <summary>Unit tests for the EF Core inbox store's transaction and dedupe behaviour.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Messaging")]
public sealed class InboxStoreTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "A new message runs the handler and persists the marker")]
    public async Task ExecuteOnceAsync_WithNewMessage_ShouldRunHandlerAndPersistMarker()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));
        Guid messageId = Guid.NewGuid();
        int calls = 0;

        // Act
        Result result = await store.ExecuteOnceAsync(messageId, "projection", _ =>
        {
            calls++;
            return Task.FromResult(Result.Ok());
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        calls.Should().Be(1);

        database.Context.ChangeTracker.Clear();
        InboxMessage stored = await database.Context.Set<InboxMessage>().SingleAsync();
        stored.MessageId.Should().Be(messageId);
        stored.Consumer.Should().Be("projection");
        stored.ProcessedOnUtc.Should().Be(Now);
    }

    [Fact(DisplayName = "An existing marker skips the handler")]
    public async Task ExecuteOnceAsync_WithExistingMarker_ShouldSkipHandler()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));
        Guid messageId = Guid.NewGuid();
        database.Context.Add(NewMarker(messageId, "projection"));
        await database.Context.SaveChangesAsync();
        int calls = 0;

        // Act
        Result result = await store.ExecuteOnceAsync(messageId, "projection", _ =>
        {
            calls++;
            return Task.FromResult(Result.Ok());
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        calls.Should().Be(0);
        (await database.Context.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A handler failure returns unchanged and leaves no marker")]
    public async Task ExecuteOnceAsync_WithHandlerFailure_ShouldReturnFailureAndLeaveNoMarker()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));
        Result failure = Result.Fail(Error.UnprocessableEntity("Test.Failed", "handler rejected"));

        // Act
        Result result = await store.ExecuteOnceAsync(
            Guid.NewGuid(), "projection", _ => Task.FromResult(failure));

        // Assert
        result.Should().BeSameAs(failure);
        (await database.Context.Set<InboxMessage>().CountAsync()).Should().Be(0);
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Fact(DisplayName = "A thrown handler exception becomes a stable internal failure")]
    public async Task ExecuteOnceAsync_WhenHandlerThrows_ShouldReturnExecutionFailed()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));

        // Act
        Result result = await store.ExecuteOnceAsync(
            Guid.NewGuid(),
            "projection",
            _ => throw new InvalidOperationException("projection exploded"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(InboxOutcome.Codes.ExecutionFailed);
        result.StatusCode.Should().Be(500);
        (await database.Context.Set<InboxMessage>().CountAsync()).Should().Be(0);
    }

    [Fact(DisplayName = "Cancellation from the handler propagates")]
    public async Task ExecuteOnceAsync_WhenHandlerCancels_ShouldPropagateCancellation()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));

        // Act
        Func<Task> act = () => store.ExecuteOnceAsync(
            Guid.NewGuid(),
            "projection",
            _ =>
            {
                // Handle: cancellation must still discard any write staged before it.
                database.Context.Set<InboxMessage>().Add(NewMarker(Guid.NewGuid(), "projection"));
                throw new OperationCanceledException();
            });

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Fact(DisplayName = "A null consumer is rejected")]
    public async Task ExecuteOnceAsync_WithNullConsumer_ShouldThrow()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));

        // Act
        Func<Task> act = () => store.ExecuteOnceAsync(
            Guid.NewGuid(), null!, _ => Task.FromResult(Result.Ok()));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact(DisplayName = "A null handler is rejected")]
    public async Task ExecuteOnceAsync_WithNullHandler_ShouldThrow()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var store = new EfCoreInboxStore(
            database.Context,
            new EfCoreUnitOfWork(database.Context),
            new FixedTimeProvider(Now));

        // Act
        Func<Task> act = () => store.ExecuteOnceAsync(Guid.NewGuid(), "projection", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "A begin failure is returned before the handler runs")]
    public async Task ExecuteOnceAsync_WhenBeginFails_ShouldReturnFailure()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(uow => uow.BeginTransactionAsync(
                It.IsAny<IsolationLevel?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ITransaction>.Fail(UnitOfWorkOutcome.Failure.ConnectionLost));
        var store = new EfCoreInboxStore(
            database.Context, unitOfWork.Object, new FixedTimeProvider(Now));
        int calls = 0;

        // Act
        Result result = await store.ExecuteOnceAsync(Guid.NewGuid(), "projection", _ =>
        {
            calls++;
            return Task.FromResult(Result.Ok());
        });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UnitOfWorkOutcome.Codes.ConnectionLost);
        calls.Should().Be(0);
    }

    [Fact(DisplayName = "A duplicate-key save with a competing marker is treated as processed")]
    public async Task ExecuteOnceAsync_WhenSaveReportsDuplicateKeyWithMarker_ShouldReturnSuccess()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<InboxTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new InboxTestContext(options);
        var transaction = CreateTransaction();
        var unitOfWork = CreateUnitOfWork(transaction);
        unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UnitOfWorkOutcome.Failure.DuplicateKey));
        var store = new EfCoreInboxStore(
            context, unitOfWork.Object, new FixedTimeProvider(Now));
        Guid messageId = Guid.NewGuid();

        // Act
        Result result = await store.ExecuteOnceAsync(messageId, "projection", async _ =>
        {
            // Handle: a competing consumer commits the same marker after the existence check.
            await using var competitor = new InboxTestContext(options);
            competitor.Add(NewMarker(messageId, "projection"));
            await competitor.SaveChangesAsync();

            return Result.Ok();
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.Verify(tx => tx.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(tx => tx.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.ChangeTracker.HasChanges().Should().BeFalse();
        (await context.Set<InboxMessage>().CountAsync()).Should().Be(1);
    }

    [Fact(DisplayName = "A duplicate-key save without a marker is a failure")]
    public async Task ExecuteOnceAsync_WhenSaveReportsDuplicateKeyWithoutMarker_ShouldReturnFailure()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var transaction = CreateTransaction();
        var unitOfWork = CreateUnitOfWork(transaction);
        unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UnitOfWorkOutcome.Failure.DuplicateKey));
        var store = new EfCoreInboxStore(
            database.Context, unitOfWork.Object, new FixedTimeProvider(Now));

        // Act
        Result result = await store.ExecuteOnceAsync(
            Guid.NewGuid(), "projection", _ => Task.FromResult(Result.Ok()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UnitOfWorkOutcome.Codes.DuplicateKey);
        transaction.Verify(tx => tx.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Fact(DisplayName = "Another save failure is returned unchanged")]
    public async Task ExecuteOnceAsync_WhenSaveFails_ShouldReturnSaveFailure()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var transaction = CreateTransaction();
        var unitOfWork = CreateUnitOfWork(transaction);
        unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UnitOfWorkOutcome.Failure.SaveChangesFailed("disk full")));
        var store = new EfCoreInboxStore(
            database.Context, unitOfWork.Object, new FixedTimeProvider(Now));

        // Act
        Result result = await store.ExecuteOnceAsync(
            Guid.NewGuid(), "projection", _ => Task.FromResult(Result.Ok()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UnitOfWorkOutcome.Codes.SaveChangesFailed);
        transaction.Verify(tx => tx.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "A commit failure rolls back and is returned")]
    public async Task ExecuteOnceAsync_WhenCommitFails_ShouldReturnCommitFailure()
    {
        // Arrange
        await using SqliteInbox database = await SqliteInbox.CreateAsync();
        var transaction = CreateTransaction(
            Result.Fail(TransactionOutcome.Failure.TransactionAlreadyCompleted));
        var unitOfWork = CreateUnitOfWork(transaction);
        unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        var store = new EfCoreInboxStore(
            database.Context, unitOfWork.Object, new FixedTimeProvider(Now));

        // Act
        Result result = await store.ExecuteOnceAsync(
            Guid.NewGuid(), "projection", _ => Task.FromResult(Result.Ok()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(TransactionOutcome.Codes.TransactionAlreadyCompleted);
        transaction.Verify(tx => tx.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static InboxMessage NewMarker(Guid messageId, string consumer) => new()
    {
        MessageId = messageId,
        Consumer = consumer,
        ProcessedOnUtc = Now,
    };

    private static Mock<ITransaction> CreateTransaction(Result? commitResult = null)
    {
        var transaction = new Mock<ITransaction>();
        transaction.SetupGet(tx => tx.State).Returns(TransactionState.Active);
        transaction.Setup(tx => tx.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(commitResult ?? Result.Ok());
        transaction.Setup(tx => tx.RollbackAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        transaction.Setup(tx => tx.DisposeAsync()).Returns(ValueTask.CompletedTask);

        return transaction;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork(Mock<ITransaction> transaction)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(uow => uow.BeginTransactionAsync(
                It.IsAny<IsolationLevel?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ITransaction>.Success(transaction.Object));

        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    // Test-only DbContext: SQLite so the store's transaction runs for real.
    private sealed class InboxTestContext(DbContextOptions<InboxTestContext> options)
        : Microsoft.EntityFrameworkCore.DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }

    private sealed class SqliteInbox : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteInbox(SqliteConnection connection, InboxTestContext context)
        {
            _connection = connection;
            Context = context;
        }

        public InboxTestContext Context { get; }

        public static async Task<SqliteInbox> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<InboxTestContext>()
                .UseSqlite(connection)
                .Options;
            var context = new InboxTestContext(options);
            await context.Database.EnsureCreatedAsync();

            return new SqliteInbox(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}