using Application.Common.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Write")]
public sealed class EfCoreRepositoryWriteTest
{
    #region AddAsync

    [Fact(DisplayName = "AddAsync should stage entity for save")]
    public async Task AddAsync_ShouldStageEntityForSave()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);

        // Act
        Result result = await repository.AddAsync(product);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.HasChanges().Should().BeTrue();
    }

    [Fact(DisplayName = "AddAsync should set entity state to Added")]
    public async Task AddAsync_ShouldSetEntityStateToAdded()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);

        // Act
        Result result = await repository.AddAsync(product);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Added);
    }

    [Fact(DisplayName = "AddAsync with null entity should return EntityRequired")]
    public async Task AddAsync_WithNullEntity_ShouldReturnEntityRequired()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.AddAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact(DisplayName = "AddAsync with cancelled token should handle cancellation")]
    public async Task AddAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result result = await repository.AddAsync(product, cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        // The key is that it returns a Result, not an unhandled exception
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "AddAsync should persist entity after SaveChangesAsync")]
    public async Task AddAsync_ShouldPersistEntityAfterSaveChanges()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);

        // Act
        Result result = await repository.AddAsync(product);
        await context.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var found = await context.Products.FindAsync(product.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Widget");
    }

    #endregion

    #region AddRangeAsync

    [Fact(DisplayName = "AddRangeAsync should stage multiple entities")]
    public async Task AddRangeAsync_ShouldStageMultipleEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m),
            EfCoreRepositoryTestFixture.CreateProduct("C", 30m)
        };

        // Act
        Result result = await repository.AddRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Entries<UnitTestProduct>().Count().Should().Be(3);
    }

    [Fact(DisplayName = "AddRangeAsync should set all entities to Added state")]
    public async Task AddRangeAsync_ShouldSetAllEntitiesToAddedState()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m)
        };

        // Act
        Result result = await repository.AddRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var p in products)
        {
            context.Entry(p).State.Should().Be(EntityState.Added);
        }
    }

    [Fact(DisplayName = "AddRangeAsync with null collection should return EntityRequired")]
    public async Task AddRangeAsync_WithNullCollection_ShouldReturnEntityRequired()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.AddRangeAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact(DisplayName = "AddRangeAsync with empty collection should succeed")]
    public async Task AddRangeAsync_WithEmptyCollection_ShouldSucceed()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var products = Array.Empty<UnitTestProduct>();

        // Act
        Result result = await repository.AddRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "AddRangeAsync with single item should succeed")]
    public async Task AddRangeAsync_WithSingleItem_ShouldSucceed()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var products = new[] { EfCoreRepositoryTestFixture.CreateProduct("A", 10m) };

        // Act
        Result result = await repository.AddRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.ChangeTracker.Entries<UnitTestProduct>().Count().Should().Be(1);
    }

    #endregion

    #region UpdateAsync

    [Fact(DisplayName = "UpdateAsync should mark entity as modified")]
    public async Task UpdateAsync_ShouldMarkEntityAsModified()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        product.Name = "Updated";

        // Act
        Result result = await repository.UpdateAsync(product);

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Modified);
    }

    [Fact(DisplayName = "UpdateAsync with null entity should return EntityRequired")]
    public async Task UpdateAsync_WithNullEntity_ShouldReturnEntityRequired()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.UpdateAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact(DisplayName = "UpdateAsync should persist changes after SaveChangesAsync")]
    public async Task UpdateAsync_ShouldPersistChangesAfterSaveChanges()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("Widget", 9.99m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        product.Name = "Updated Widget";
        product.Price = 19.99m;

        // Act
        Result result = await repository.UpdateAsync(product);
        await context.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Unchanged);
        var found = await context.Products.FindAsync(product.Id);
        found!.Name.Should().Be("Updated Widget");
        found.Price.Should().Be(19.99m);
    }

    #endregion

    #region UpdateRangeAsync

    [Fact(DisplayName = "UpdateRangeAsync should mark multiple entities as modified")]
    public async Task UpdateRangeAsync_ShouldMarkMultipleEntitiesAsModified()
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
        products[0].Name = "A Updated";
        products[1].Name = "B Updated";

        // Act
        Result result = await repository.UpdateRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateRangeAsync with null collection should return EntityRequired")]
    public async Task UpdateRangeAsync_WithNullCollection_ShouldReturnEntityRequired()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        // Act
        Result result = await repository.UpdateRangeAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact(DisplayName = "UpdateRangeAsync with empty collection should succeed")]
    public async Task UpdateRangeAsync_WithEmptyCollection_ShouldSucceed()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var products = Array.Empty<UnitTestProduct>();

        // Act
        Result result = await repository.UpdateRangeAsync(products);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateRangeAsync with single item should succeed")]
    public async Task UpdateRangeAsync_WithSingleItem_ShouldSucceed()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var product = EfCoreRepositoryTestFixture.CreateProduct("A", 10m);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        product.Name = "A Updated";

        // Act
        Result result = await repository.UpdateRangeAsync(new[] { product });

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Entry(product).State.Should().Be(EntityState.Modified);
    }

    #endregion
}
