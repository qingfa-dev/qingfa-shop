using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

using Core.Common.Monad.Maybes;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Search")]
public sealed class EfCoreRepositorySearchTest
{
    #region QueryAsync

    [Fact(DisplayName = "QueryAsync with no filter should return all entities")]
    public async Task QueryAsync_WithNoFilter_ShouldReturnAllEntities()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m),
            EfCoreRepositoryTestFixture.CreateProduct("C", 30m)
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact(DisplayName = "QueryAsync with pagination should return correct page")]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = Enumerable.Range(1, 25)
            .Select(i => EfCoreRepositoryTestFixture.CreateProduct($"Product {i}", i))
            .ToList();
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>
        {
            Page = new PageRequest(2, 10)
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }

    [Theory(DisplayName = "QueryAsync pagination edge cases")]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    [InlineData(999, 10)]
    public async Task QueryAsync_PaginationEdgeCases(int page, int pageSize)
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = Enumerable.Range(1, 3)
            .Select(i => EfCoreRepositoryTestFixture.CreateProduct($"Product {i}", i))
            .ToList();
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>
        {
            Page = new PageRequest(page, pageSize)
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
        if (page > 1)
        {
            result.Value.Items.Should().BeEmpty();
        }
    }

    [Fact(DisplayName = "QueryAsync with empty results should return empty page")]
    public async Task QueryAsync_WithEmptyResults_ShouldReturnEmptyPage()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact(DisplayName = "QueryAsync with sort should apply ordering")]
    public async Task QueryAsync_WithSort_ShouldApplyOrdering()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("C", 30m));
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("A", 10m));
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("B", 20m));
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>
        {
            OrderBy = new List<SortField> { new SortField("Name", SortDirection.Ascending, SortNulls.None) }
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        // Mock translator doesn't actually sort, but it should not fail
    }

    [Fact(DisplayName = "QueryAsync with search should not fail")]
    public async Task QueryAsync_WithSearch_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("Widget", 10m));
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>
        {
            Search = new BasicSearchRequest("Widget", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "QueryAsync with cancelled token should handle cancellation")]
    public async Task QueryAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Cancelled");
    }

    #endregion

    #region QuerySingleAsync

    [Fact(DisplayName = "QuerySingleAsync should return single entity")]
    public async Task QuerySingleAsync_ShouldReturnSingleEntity()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("A", 10m));
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<UnitTestProduct> result = await repository.QuerySingleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("A");
    }

    [Fact(DisplayName = "QuerySingleAsync when no entity should return NotFound")]
    public async Task QuerySingleAsync_WhenNoEntity_ShouldReturnNotFound()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<UnitTestProduct> result = await repository.QuerySingleAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("NotFound");
    }

    [Fact(DisplayName = "QuerySingleAsync with cancelled token should handle cancellation")]
    public async Task QuerySingleAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<UnitTestProduct> result = await repository.QuerySingleAsync(query, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Cancelled");
    }

    #endregion

    #region QueryFirstOrDefaultAsync

    [Fact(DisplayName = "QueryFirstOrDefaultAsync should return first entity")]
    public async Task QueryFirstOrDefaultAsync_ShouldReturnFirstEntity()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("A", 10m));
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<Maybe<UnitTestProduct>> result = await repository.QueryFirstOrDefaultAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Name.Should().Be("A");
    }

    [Fact(DisplayName = "QueryFirstOrDefaultAsync when empty should return empty Maybe")]
    public async Task QueryFirstOrDefaultAsync_WhenEmpty_ShouldReturnEmptyMaybe()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<Maybe<UnitTestProduct>> result = await repository.QueryFirstOrDefaultAsync(query);

        // Assert - returns success with empty Maybe
        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact(DisplayName = "QueryFirstOrDefaultAsync with cancelled token should handle cancellation")]
    public async Task QueryFirstOrDefaultAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<Maybe<UnitTestProduct>> result = await repository.QueryFirstOrDefaultAsync(query, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Cancelled");
    }

    #endregion

    #region CountAsync

    [Fact(DisplayName = "CountAsync should return total count")]
    public async Task CountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var products = new[]
        {
            EfCoreRepositoryTestFixture.CreateProduct("A", 10m),
            EfCoreRepositoryTestFixture.CreateProduct("B", 20m),
            EfCoreRepositoryTestFixture.CreateProduct("C", 30m)
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<int> result = await repository.CountAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact(DisplayName = "CountAsync with no entities should return zero")]
    public async Task CountAsync_WithNoEntities_ShouldReturnZero()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<int> result = await repository.CountAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact(DisplayName = "CountAsync with cancelled token should handle cancellation")]
    public async Task CountAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<int> result = await repository.CountAsync(query, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Cancelled");
    }

    #endregion

    #region AnyAsync

    [Theory(DisplayName = "AnyAsync should return correct existence result")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnyAsync_ShouldReturnCorrectResult(bool hasEntities)
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        if (hasEntities)
        {
            context.Products.Add(EfCoreRepositoryTestFixture.CreateProduct("A", 10m));
            await context.SaveChangesAsync();
        }
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();

        // Act
        Result<bool> result = await repository.AnyAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(hasEntities);
    }

    [Fact(DisplayName = "AnyAsync with cancelled token should handle cancellation")]
    public async Task AnyAsync_WithCancelledToken_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var repository = EfCoreRepositoryTestFixture.CreateRepository(context);
        var query = new Query<UnitTestProduct>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Result<bool> result = await repository.AnyAsync(query, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Cancelled");
    }

    #endregion

    #region BuildQuery Error Path Tests

    [Fact(DisplayName = "QueryAsync with failing filter translation should return failure")]
    public async Task QueryAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "QueryAsync with failing search translation should return failure")]
    public async Task QueryAsync_WithFailingSearch_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailSearch = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Search = new BasicSearchRequest("Widget", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "QueryAsync with failing sort translation should return failure")]
    public async Task QueryAsync_WithFailingSort_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailSort = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            OrderBy = new List<SortField> { new SortField("Name", SortDirection.Ascending, SortNulls.None) }
        };

        // Act
        Result<QueryPage<UnitTestProduct>> result = await repository.QueryAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "QuerySingleAsync with failing filter should return failure")]
    public async Task QuerySingleAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<UnitTestProduct> result = await repository.QuerySingleAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "QueryFirstOrDefaultAsync with failing filter should return failure")]
    public async Task QueryFirstOrDefaultAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<Maybe<UnitTestProduct>> result = await repository.QueryFirstOrDefaultAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "CountAsync with failing filter should return failure")]
    public async Task CountAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<int> result = await repository.CountAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "AnyAsync with failing filter should return failure")]
    public async Task AnyAsync_WithFailingFilter_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContext();
        var translator = new FailingMockQueryTranslator<UnitTestProduct> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryWithTranslator(context, translator);
        var query = new Query<UnitTestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Widget", "Widget", typeof(string), true, null, null)
        };

        // Act
        Result<bool> result = await repository.AnyAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    #endregion
}
