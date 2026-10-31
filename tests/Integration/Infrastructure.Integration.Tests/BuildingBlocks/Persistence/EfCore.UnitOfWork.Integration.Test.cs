using System.Data;
using Core.BuildingBlocks.Functional.Results;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Persistence;

[Collection(EfCoreCollection.Name)]
public sealed class EfCoreUnitOfWorkIntegrationTest : IAsyncLifetime
{
    private readonly EfCoreFixture _fixture;

    public EfCoreUnitOfWorkIntegrationTest(EfCoreFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.CleanAsync();
        _fixture.CreateScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.CleanAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WithChanges_PersistsThem()
    {
        // Arrange
        var product = CreateProduct("Widget Save", 9.99m);
        _fixture.DbContext.Products.Add(product);

        // Act
        Result result = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task HasChanges_ReturnsCorrectState()
    {
        // Arrange
        var product = CreateProduct("Widget Track", 9.99m);
        _fixture.DbContext.Products.Add(product);

        // Assert - has changes before save
        _fixture.UnitOfWork.HasChanges.Should().BeTrue();

        // Act - save
        await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert - no changes after save
        _fixture.UnitOfWork.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task BeginTransactionAsync_CreatesTransaction()
    {
        // Act
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> result
            = await _fixture.UnitOfWork.BeginTransactionAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.State.Should().Be(Application.BuildingBlocks.Persistence.UnitOfWorks.TransactionState.Active);

        await result.Value.RollbackAsync();
        await result.Value.DisposeAsync();
    }

    [Fact]
    public async Task BeginTransactionAsync_WithIsolationLevel_CreatesTransaction()
    {
        // Act
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> result
            = await _fixture.UnitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsolationLevel.Should().Be(IsolationLevel.Serializable);

        await result.Value.RollbackAsync();
        await result.Value.DisposeAsync();
    }

    [Fact]
    public async Task CommitAsync_PersistsChanges()
    {
        // Arrange
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> txResult
            = await _fixture.UnitOfWork.BeginTransactionAsync();
        txResult.IsSuccess.Should().BeTrue();

        var product = CreateProduct("Widget Commit", 19.99m);
        _fixture.DbContext.Products.Add(product);

        // Act
        Result commitResult = await txResult.Value.CommitAsync();

        // Assert
        commitResult.IsSuccess.Should().BeTrue();
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeTrue();

        await txResult.Value.DisposeAsync();
    }

    [Fact]
    public async Task RollbackAsync_DiscardsChanges()
    {
        // Arrange
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> txResult
            = await _fixture.UnitOfWork.BeginTransactionAsync();
        txResult.IsSuccess.Should().BeTrue();

        var product = CreateProduct("Widget Rollback", 29.99m);
        _fixture.DbContext.Products.Add(product);

        // Act
        Result rollbackResult = await txResult.Value.RollbackAsync();

        // Assert
        rollbackResult.IsSuccess.Should().BeTrue();
        txResult.Value.State.Should().Be(Application.BuildingBlocks.Persistence.UnitOfWorks.TransactionState.RolledBack);

        await txResult.Value.DisposeAsync();

        // Verify the product was not persisted
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeFalse();
    }

    [Fact]
    public async Task MarkRollbackOnly_PreventsCommit()
    {
        // Arrange
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> txResult
            = await _fixture.UnitOfWork.BeginTransactionAsync();
        txResult.IsSuccess.Should().BeTrue();

        var product = CreateProduct("Widget RollbackOnly", 39.99m);
        _fixture.DbContext.Products.Add(product);

        // Act
        txResult.Value.MarkRollbackOnly();
        Result commitResult = await txResult.Value.CommitAsync();

        // Assert
        commitResult.IsFailure.Should().BeTrue();
        commitResult.Errors.First().Code.Should().Contain("RollbackOnly");

        await txResult.Value.RollbackAsync();
        await txResult.Value.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Success_SavesChanges()
    {
        // Arrange
        var product = CreateProduct("Widget Execute", 49.99m);

        // Act
        Result result = await _fixture.UnitOfWork.ExecuteAsync(async ct =>
        {
            _fixture.DbContext.Products.Add(product);
            await Task.CompletedTask;
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithReturnValue_ReturnsValue()
    {
        // Arrange
        var product = CreateProduct("Widget ExecuteReturn", 59.99m);

        // Act
        Result<int> result = await _fixture.UnitOfWork.ExecuteAsync(async ct =>
        {
            _fixture.DbContext.Products.Add(product);
            return 42;
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_Success_Commits()
    {
        // Arrange
        var product = CreateProduct("Widget TxExecute", 69.99m);

        // Act
        Result result = await _fixture.UnitOfWork.ExecuteInTransactionAsync(async (tx, ct) =>
        {
            tx.State.Should().Be(Application.BuildingBlocks.Persistence.UnitOfWorks.TransactionState.Active);
            _fixture.DbContext.Products.Add(product);
            await Task.CompletedTask;
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithReturnValue_ReturnsValue()
    {
        // Arrange
        var product = CreateProduct("Widget TxReturn", 79.99m);

        // Act
        Result<string> result = await _fixture.UnitOfWork.ExecuteInTransactionAsync(async (tx, ct) =>
        {
            _fixture.DbContext.Products.Add(product);
            return "success";
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("success");
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeTrue();
    }

    [Fact]
    public async Task NestedTransaction_ReturnsFailure()
    {
        // Arrange
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> tx1Result
            = await _fixture.UnitOfWork.BeginTransactionAsync();
        tx1Result.IsSuccess.Should().BeTrue();

        // Act
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> tx2Result
            = await _fixture.UnitOfWork.BeginTransactionAsync();

        // Assert
        tx2Result.IsFailure.Should().BeTrue();
        tx2Result.Errors.First().Code.Should().Contain("Nested");

        await tx1Result.Value.RollbackAsync();
        await tx1Result.Value.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenWorkThrows_Rollbacks()
    {
        // Arrange
        var product = CreateProduct("Widget Rollback", 99.99m);

        // Act
        Result result = await _fixture.UnitOfWork.ExecuteInTransactionAsync(async (tx, ct) =>
        {
            _fixture.DbContext.Products.Add(product);
            throw new InvalidOperationException("Test error");
        });

        // Assert: should return failure, not throw
        result.IsFailure.Should().BeTrue();

        // Verify rollback: product should not be persisted
        (await _fixture.Repository.ExistsAsync(product.Id)).Value.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullWork_ReturnsFailure()
    {
        // Act
        Result result = await _fixture.UnitOfWork.ExecuteAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Work.Required");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithNullWork_ReturnsFailure()
    {
        // Act
        Result result = await _fixture.UnitOfWork.ExecuteInTransactionAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Work.Required");
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_Succeeds()
    {
        // Act
        Result result = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    public async Task BeginTransactionAsync_WithIsolationLevel_ShouldCreateTransaction(IsolationLevel isolationLevel)
    {
        // Act
        Result<Application.BuildingBlocks.Persistence.UnitOfWorks.ITransaction> result
            = await _fixture.UnitOfWork.BeginTransactionAsync(isolationLevel);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsolationLevel.Should().Be(isolationLevel);

        await result.Value.RollbackAsync();
        await result.Value.DisposeAsync();
    }

    private static TestProduct CreateProduct(string name, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = $"Description for {name}",
        Price = price,
        Stock = 100,
        CreatedAt = DateTimeOffset.UtcNow,
        IsActive = true
    };
}
