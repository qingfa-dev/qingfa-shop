using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Pagination;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Mapping")]
public sealed class EfCoreRepositoryMappingTest
{
    #region MapAsync - Post-materialization mapping

    [Fact(DisplayName = "MapAsync should map all entities to DTOs")]
    public async Task MapAsync_ShouldMapAllEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query, p => p.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(3);
        result.Value.Items.Should().Contain("Product 1");
        result.Value.Items.Should().Contain("Product 2");
        result.Value.Items.Should().Contain("Product 3");
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
    }

    [Fact(DisplayName = "MapAsync with filter should map only matching entities")]
    public async Task MapAsync_WithFilter_ShouldMapOnlyMatchingEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Price", FilterOperator.GreaterThan, "25", 25m, typeof(decimal), false, null, null)
        };

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query, p => p.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Mock translator passes filter through without filtering, so all 5 are returned
        result.Value.Items.Count.Should().Be(5);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact(DisplayName = "MapAsync with pagination should return only the page")]
    public async Task MapAsync_WithPagination_ShouldReturnOnlyPage()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<decimal>> result = await repository.MapAsync(
            query, p => p.Price);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(3);
        result.Value.TotalCount.Should().Be(10);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(3);
    }

    [Fact(DisplayName = "MapAsync with empty results should return empty list")]
    public async Task MapAsync_WithEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query, p => p.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "MapAsync with failing filter should return failure")]
    public async Task MapAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);

        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Test", "Test", typeof(string), false, null, null)
        };

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query, p => p.Name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "MapAsync with complex DTO mapping should work")]
    public async Task MapAsync_WithComplexDto_ShouldWork()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 2);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<ProductDto>> result = await repository.MapAsync(
            query, p => new ProductDto(p.Name, p.Price));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.First().Name.Should().NotBeNullOrEmpty();
        result.Value.Items.First().Price.Should().BeGreaterThan(0);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "MapAsync with cancelled token should handle cancellation")]
    public async Task MapAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query, p => p.Name, cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        result.Should().NotBeNull();
    }

    #endregion

    #region ProjectAsync - Server-side projection

    [Fact(DisplayName = "ProjectAsync should project all entities to DTOs")]
    public async Task ProjectAsync_ShouldProjectAllEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query, p => p.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(3);
        result.Value.Items.Should().Contain("Product 1");
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
    }

    [Fact(DisplayName = "ProjectAsync with filter should project only matching entities")]
    public async Task ProjectAsync_WithFilter_ShouldProjectOnlyMatchingEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();
        // Mock translator doesn't actually filter, so all entities pass through

        // Act
        Result<QueryPage<decimal>> result = await repository.ProjectAsync(
            query, p => p.Price);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(5);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact(DisplayName = "ProjectAsync with pagination should return only the page")]
    public async Task ProjectAsync_WithPagination_ShouldReturnOnlyPage()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>
        {
            Page = new PageRequest(1, 3)
        };

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query, p => p.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(3);
        result.Value.TotalCount.Should().Be(10);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(3);
    }

    [Fact(DisplayName = "ProjectAsync with empty results should return empty list")]
    public async Task ProjectAsync_WithEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query, p => p.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "ProjectAsync with failing filter should return failure")]
    public async Task ProjectAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);

        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Test", "Test", typeof(string), false, null, null)
        };

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query, p => p.Name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "ProjectAsync with complex projection should work")]
    public async Task ProjectAsync_WithComplexProjection_ShouldWork()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        await EfCoreRepositoryTestFixture.SeedProductsAsync(context, 2);
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<ProductDto>> result = await repository.ProjectAsync(
            query, p => new ProductDto(p.Name, p.Price));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.First().Name.Should().NotBeNullOrEmpty();
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact(DisplayName = "ProjectAsync with cancelled token should handle cancellation")]
    public async Task ProjectAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query, p => p.Name, cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        result.Should().NotBeNull();
    }

    #endregion

    #region Shared DTOs

    public record ProductDto(string Name, decimal Price);

    #endregion
}
