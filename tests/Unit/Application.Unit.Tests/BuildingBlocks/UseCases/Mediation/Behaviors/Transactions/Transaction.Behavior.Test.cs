using System.Data;

using Application.BuildingBlocks.UseCases.Mediation.Behaviors.Transactions;
using Application.BuildingBlocks.UseCases.Mediation.Commands;
using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using MediatR;

using Moq;

namespace Application.Unit.Tests.BuildingBlocks.UseCases.Mediation.Behaviors.Transactions;

/// <summary>Tests for the command transaction behavior.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Mediators")]
public sealed class TransactionBehaviorTest
{
    [Fact(DisplayName = "Command success should commit and return the response")]
    public async Task Handle_WhenCommandSucceeds_ShouldCommit()
    {
        // Arrange
        var transaction = new FakeTransaction();
        var unitOfWork = CreateUnitOfWork(transaction);
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(unitOfWork.Object);

        // Act
        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result<string>.Success("ok")),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
        transaction.Commits.Should().Be(1);
        transaction.Rollbacks.Should().Be(0);
        transaction.Disposed.Should().BeTrue();
    }

    [Fact(DisplayName = "Command failure should roll back and preserve errors")]
    public async Task Handle_WhenCommandFails_ShouldRollback()
    {
        // Arrange
        var transaction = new FakeTransaction();
        var unitOfWork = CreateUnitOfWork(transaction);
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(unitOfWork.Object);
        var failure = Result<string>.Fail(Error.Validation("command.failed", "no"));

        // Act
        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(failure),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "command.failed");
        transaction.Commits.Should().Be(0);
        transaction.Rollbacks.Should().Be(1);
    }

    [Fact(DisplayName = "Query should bypass the transaction")]
    public async Task Handle_WhenQuery_ShouldNotBeginTransaction()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestQuery, Result<string>>(unitOfWork.Object);

        // Act
        Result<string> result = await behavior.Handle(
            new TestQuery(),
            _ => Task.FromResult(Result<string>.Success("query")),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        unitOfWork.Verify(
            work => work.BeginTransactionAsync(
                It.IsAny<IsolationLevel?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Non-transactional command should bypass the transaction")]
    public async Task Handle_WhenNonTransactionalCommand_ShouldNotBeginTransaction()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestNonTransactionalCommand, Result<string>>(
            unitOfWork.Object);

        // Act
        Result<string> result = await behavior.Handle(
            new TestNonTransactionalCommand(),
            _ => Task.FromResult(Result<string>.Success("own-boundary")),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("own-boundary");
        unitOfWork.Verify(
            work => work.BeginTransactionAsync(
                It.IsAny<IsolationLevel?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Exception should roll back and rethrow")]
    public async Task Handle_WhenHandlerThrows_ShouldRollbackAndRethrow()
    {
        // Arrange
        var transaction = new FakeTransaction();
        var unitOfWork = CreateUnitOfWork(transaction);
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(unitOfWork.Object);

        // Act
        Func<Task> act = () => behavior.Handle(
            new TestCommand(),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Rollbacks.Should().Be(1);
        transaction.Commits.Should().Be(0);
    }

    [Fact(DisplayName = "Begin failure should return a failed result without calling the handler")]
    public async Task Handle_WhenBeginFails_ShouldReturnFailure()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.BeginTransactionAsync(
                It.IsAny<IsolationLevel?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ITransaction>.Fail(Error.InternalServerError("begin", "failed")));
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(unitOfWork.Object);
        var called = false;

        // Act
        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ =>
            {
                called = true;
                return Task.FromResult(Result<string>.Success("no"));
            },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        called.Should().BeFalse();
    }

    [Fact(DisplayName = "Commit failure should return a failed result")]
    public async Task Handle_WhenCommitFails_ShouldReturnFailure()
    {
        // Arrange
        var transaction = new FakeTransaction
        {
            CommitResult = Result.Fail(Error.InternalServerError("commit", "failed")),
        };
        var unitOfWork = CreateUnitOfWork(transaction);
        var behavior = new TransactionBehavior<TestCommand, Result<string>>(unitOfWork.Object);

        // Act
        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result<string>.Success("ok")),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        transaction.Commits.Should().Be(1);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork(FakeTransaction transaction)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.BeginTransactionAsync(
                It.IsAny<IsolationLevel?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ITransaction>.Success(transaction));
        return unitOfWork;
    }

    private sealed record TestCommand : ICommand<string>;

    private sealed record TestNonTransactionalCommand
        : ICommand<string>, INonTransactionalCommand;

    private sealed record TestQuery : IQuery<string>;

    private sealed class FakeTransaction : ITransaction
    {
        public TransactionState State { get; private set; } = TransactionState.Active;
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        public bool IsReadOnly => false;
        public int Commits { get; private set; }
        public int Rollbacks { get; private set; }
        public bool Disposed { get; private set; }
        public Result CommitResult { get; set; } = Result.Ok();
        public Result RollbackResult { get; set; } = Result.Ok();

        public Task<Result> CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;
            State = CommitResult.IsSuccess ? TransactionState.Committed : TransactionState.RollbackOnly;
            return Task.FromResult(CommitResult);
        }

        public Task<Result> RollbackAsync(CancellationToken cancellationToken = default)
        {
            Rollbacks++;
            State = TransactionState.RolledBack;
            return Task.FromResult(RollbackResult);
        }

        public Result MarkRollbackOnly()
        {
            State = TransactionState.RollbackOnly;
            return Result.Ok();
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
