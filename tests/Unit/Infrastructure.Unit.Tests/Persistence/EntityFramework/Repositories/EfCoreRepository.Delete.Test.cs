using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Delete")]
public sealed class EfCoreRepositoryDeleteTest
{
    #region RemoveAsync by Entity

    [Fact(DisplayName = "RemoveAsync by entity should stage for deletion")]
    public async Task RemoveAsync_ByEntity_ShouldStageForDeletion()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveAsync(product);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Deleted);
    }

    [Fact(DisplayName = "RemoveAsync by entity with null should return EntityRequired")]
    public async Task RemoveAsync_ByEntity_WithNull_ShouldReturnEntityRequired()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact(DisplayName = "RemoveAsync by entity should persist deletion after SaveChangesAsync")]
    public async Task RemoveAsync_ByEntity_ShouldPersistDeletion()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveAsync(product);
        await context.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var found = await context.Products.FindAsync(product.Id);
        found.Should().BeNull();
    }

    #endregion

    #region RemoveAsync by Id

    [Fact(DisplayName = "RemoveAsync by id when entity exists should stage for deletion")]
    public async Task RemoveAsync_ById_WhenEntityExists_ShouldStageForDeletion()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveAsync(product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Deleted);
    }

    [Fact(DisplayName = "RemoveAsync by id when entity not found should return NotFound")]
    public async Task RemoveAsync_ById_WhenEntityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        Guid nonExistentId = Guid.NewGuid();

        // Act
        Result result = await repository.RemoveAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("NotFound");
    }

    [Fact(DisplayName = "RemoveAsync by id with Guid.Empty should return NotFound")]
    public async Task RemoveAsync_ById_WithGuidEmpty_ShouldReturnNotFound()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("NotFound");
    }

    [Fact(DisplayName = "RemoveAsync by id should persist deletion after SaveChangesAsync")]
    public async Task RemoveAsync_ById_ShouldPersistDeletion()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveAsync(product.Id);
        await context.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var found = await context.Products.FindAsync(product.Id);
        found.Should().BeNull();
    }

    [Fact(DisplayName = "RemoveAsync by id with cancelled token should handle cancellation")]
    public async Task RemoveAsync_ById_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await repository.RemoveAsync(Guid.NewGuid(), cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Cancelled");
    }

    #endregion

    #region RemoveRangeAsync

    [Fact(DisplayName = "RemoveRangeAsync should stage multiple entities for deletion")]
    public async Task RemoveRangeAsync_ShouldStageMultipleEntitiesForDeletion()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m)
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var p in products)
        {
            context.Entry(p).State.Should().Be(EntityState.Deleted);
        }
    }

    [Fact(DisplayName = "RemoveRangeAsync with null collection should return EntityRequired")]
    public async Task RemoveRangeAsync_WithNullCollection_ShouldReturnEntityRequired()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveRangeAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact(DisplayName = "RemoveRangeAsync with empty collection should succeed")]
    public async Task RemoveRangeAsync_WithEmptyCollection_ShouldSucceed()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var products = Array.Empty<UnitTestProduct>();

        // Act
        Result result = await repository.RemoveRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "RemoveRangeAsync with single item should succeed")]
    public async Task RemoveRangeAsync_WithSingleItem_ShouldSucceed()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("A", 10m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveRangeAsync(new[] { product });

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Deleted);
    }

    [Fact(DisplayName = "RemoveRangeAsync should persist deletions after SaveChangesAsync")]
    public async Task RemoveRangeAsync_ShouldPersistDeletions()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m)
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.RemoveRangeAsync(products);
        await context.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var p in products)
        {
            var found = await context.Products.FindAsync(p.Id);
            found.Should().BeNull();
        }
    }

    #endregion
}
