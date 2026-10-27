using Application.Common.Persistence.UnitOfWorks;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.UnitOfWorks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.UnitOfWorks;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class EfCoreTransactionTest
{
    private (EfCoreDbContext Context, EfCoreTransaction Transaction) CreateTransaction(bool readOnly = false)
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new UowTestDbContext(options);

        // Create a real transaction via the DbContext
        IDbContextTransaction dbTransaction = context.Database.BeginTransaction();
        var transaction = new EfCoreTransaction(context, dbTransaction, readOnly);

        return (context, transaction);
    }

    #region State Management

    [Fact(DisplayName = "Transaction state should be Active initially")]
    public void Transaction_State_ShouldBeActiveInitially()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();

        // Assert
        transaction.State.Should().Be(TransactionState.Active);
    }

    [Fact(DisplayName = "Transaction IsReadOnly should reflect the constructor parameter")]
    public void Transaction_IsReadOnly_ShouldReflectConstructorParameter()
    {
        // Arrange
        var (_, readOnlyTx) = CreateTransaction(readOnly: true);
        var (_, writableTx) = CreateTransaction(readOnly: false);

        // Assert
        readOnlyTx.IsReadOnly.Should().BeTrue();
        writableTx.IsReadOnly.Should().BeFalse();
    }

    [Fact(DisplayName = "MarkRollbackOnly should change state to RollbackOnly")]
    public void MarkRollbackOnly_ShouldChangeStateToRollbackOnly()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();

        // Act
        transaction.MarkRollbackOnly();

        // Assert
        transaction.State.Should().Be(TransactionState.RollbackOnly);
    }

    [Fact(DisplayName = "MarkRollbackOnly when already committed should fail")]
    public async Task MarkRollbackOnly_WhenAlreadyCommitted_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.CommitAsync();

        // Act & Assert
        Result result = transaction.MarkRollbackOnly();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "MarkRollbackOnly when disposed should fail")]
    public async Task MarkRollbackOnly_WhenDisposed_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.DisposeAsync();

        // Act & Assert
        Result result = transaction.MarkRollbackOnly();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");
    }

    #endregion

    #region CommitAsync

    [Fact(DisplayName = "CommitAsync should change state to Committed")]
    public async Task CommitAsync_ShouldChangeStateToCommitted()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.Committed);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync when RollbackOnly should fail")]
    public async Task CommitAsync_WhenRollbackOnly_ShouldFail()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        transaction.MarkRollbackOnly();

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("RollbackOnly");

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync when already committed should fail")]
    public async Task CommitAsync_WhenAlreadyCommitted_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.CommitAsync();

        // Act & Assert
        Result result = await transaction.CommitAsync();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync when disposed should fail")]
    public async Task CommitAsync_WhenDisposed_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.DisposeAsync();

        // Act & Assert
        Result result = await transaction.CommitAsync();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");
    }

    [Fact(DisplayName = "CommitAsync with cancelled token should handle gracefully")]
    public async Task CommitAsync_WithCancelledToken_ShouldHandleGracefully()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await transaction.CommitAsync(cts.Token);

        // Assert - InMemory provider ignores cancellation, so commit succeeds
        result.IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.Committed);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync when already rolled back should fail")]
    public async Task CommitAsync_WhenAlreadyRolledBack_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.RollbackAsync();

        // Act & Assert
        Result result = await transaction.CommitAsync();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "CommitAsync should save pending changes before committing")]
    public async Task CommitAsync_ShouldSavePendingChanges()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        var product = new UowTestProduct
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = 9.99m
        };
        var uowContext = (UowTestDbContext)context;
        uowContext.Products.Add(product);

        // Act
        Result result = await transaction.CommitAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.Committed);
        (await uowContext.Products.AnyAsync(p => p.Id == product.Id)).Should().BeTrue();

        await transaction.DisposeAsync();
    }

    #endregion

    #region RollbackAsync

    [Fact(DisplayName = "RollbackAsync should change state to RolledBack")]
    public async Task RollbackAsync_ShouldChangeStateToRolledBack()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();

        // Act
        Result result = await transaction.RollbackAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.RolledBack);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "RollbackAsync when already rolled back should fail")]
    public async Task RollbackAsync_WhenAlreadyRolledBack_ShouldFail()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        await transaction.RollbackAsync();

        // Act & Assert
        Result result = await transaction.RollbackAsync();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "RollbackAsync with cancelled token should handle gracefully")]
    public async Task RollbackAsync_WithCancelledToken_ShouldHandleGracefully()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await transaction.RollbackAsync(cts.Token);

        // Assert - InMemory provider ignores cancellation, so rollback succeeds
        result.IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.RolledBack);

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "RollbackAsync when already committed should fail")]
    public async Task RollbackAsync_WhenAlreadyCommitted_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.CommitAsync();

        // Act & Assert
        Result result = await transaction.RollbackAsync();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");

        await transaction.DisposeAsync();
    }

    [Fact(DisplayName = "RollbackAsync when disposed should fail")]
    public async Task RollbackAsync_WhenDisposed_ShouldFail()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();
        await transaction.DisposeAsync();

        // Act & Assert
        Result result = await transaction.RollbackAsync();
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("AlreadyCompleted");
    }

    [Fact(DisplayName = "RollbackAsync when RollbackOnly should succeed")]
    public async Task RollbackAsync_WhenRollbackOnly_ShouldSucceed()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        transaction.MarkRollbackOnly();

        // Act
        Result result = await transaction.RollbackAsync();

        // Assert - EnsureActive allows RollbackOnly, so rollback proceeds
        result.IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.RolledBack);

        await transaction.DisposeAsync();
    }

    #endregion

    #region DisposeAsync

    [Fact(DisplayName = "DisposeAsync when active should rollback")]
    public async Task DisposeAsync_WhenActive_ShouldRollback()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();

        // Act
        await transaction.DisposeAsync();

        // Assert
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "DisposeAsync when committed should not rollback")]
    public async Task DisposeAsync_WhenCommitted_ShouldNotRollback()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        await transaction.CommitAsync();

        // Act
        await transaction.DisposeAsync();

        // Assert
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "DisposeAsync when rolled back should not rollback again")]
    public async Task DisposeAsync_WhenRolledBack_ShouldNotRollbackAgain()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        await transaction.RollbackAsync();

        // Act
        await transaction.DisposeAsync();

        // Assert
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "DisposeAsync should be idempotent")]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();

        // Act
        await transaction.DisposeAsync();
        await transaction.DisposeAsync();

        // Assert - should not throw
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "DisposeAsync when RollbackOnly should rollback")]
    public async Task DisposeAsync_WhenRollbackOnly_ShouldRollback()
    {
        // Arrange
        var (_, transaction) = CreateTransaction();
        transaction.MarkRollbackOnly();

        // Act
        await transaction.DisposeAsync();

        // Assert
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "Transaction state transitions should follow valid paths")]
    public async Task Transaction_StateTransitions_ShouldFollowValidPaths()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();

        // Act & Assert: Active -> Committed -> Disposed
        transaction.State.Should().Be(TransactionState.Active);
        (await transaction.CommitAsync()).IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.Committed);
        await transaction.DisposeAsync();
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "Transaction state transitions should allow Active -> RolledBack -> Disposed")]
    public async Task Transaction_StateTransitions_ShouldAllowActiveRolledBackDisposed()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();

        // Act & Assert: Active -> RolledBack -> Disposed
        transaction.State.Should().Be(TransactionState.Active);
        (await transaction.RollbackAsync()).IsSuccess.Should().BeTrue();
        transaction.State.Should().Be(TransactionState.RolledBack);
        await transaction.DisposeAsync();
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    [Fact(DisplayName = "Transaction state transitions should allow Active -> RollbackOnly -> RolledBack via Dispose")]
    public async Task Transaction_StateTransitions_ShouldAllowActiveRollbackOnlyDisposed()
    {
        // Arrange
        var (context, transaction) = CreateTransaction();

        // Act & Assert: Active -> RollbackOnly -> Disposed (auto-rollback)
        transaction.State.Should().Be(TransactionState.Active);
        transaction.MarkRollbackOnly();
        transaction.State.Should().Be(TransactionState.RollbackOnly);
        await transaction.DisposeAsync();
        transaction.State.Should().Be(TransactionState.Disposed);
    }

    #endregion
}
