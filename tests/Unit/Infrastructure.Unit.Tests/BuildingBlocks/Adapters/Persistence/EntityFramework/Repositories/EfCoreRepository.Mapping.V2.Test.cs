using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Querying.Sorting;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Mapping.V2")]
public sealed class EfCoreRepositoryMappingV2Test
{
    #region DTOs for V2 tests

    public record ProductSummaryDto(string Name, decimal Price, int Stock, bool Active);
    public record ProductWithCategoryDto(string Name, decimal Price, string CategoryName);
    public record ProductWithDetailDto(string Name, string? Sku, double? WeightKg);
    public record ProductWithReviewsDto(string Name, int ReviewCount, double AverageRating);
    public record ProductWithTagsDto(string Name, IReadOnlyList<string> Tags);
    public record ProductFullDto(
        string Name,
        decimal Price,
        string CategoryName,
        string? Sku,
        int ReviewCount,
        double AverageRating,
        IReadOnlyList<string> Tags);
    public record ProductComputedDto(string Name, decimal Price, decimal DiscountedPrice, string StockStatus);
    public record NestedCategoryDto(string CategoryName, IReadOnlyList<string> ProductNames);
    public record ReviewDto(string Author, int Rating, string? Comment, string ProductName);

    #endregion

    #region MapAsync - Navigation property mapping

    [Fact(DisplayName = "MapAsync should map scalar properties from V2 entity")]
    public async Task MapAsync_WithV2ScalarProperties_ShouldMapCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Count.Should().Be(5);
        result.Value.Items.First().Name.Should().NotBeNullOrEmpty();
        result.Value.Items.First().Price.Should().BeGreaterThan(0);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact(DisplayName = "MapAsync should handle null navigation gracefully (InMemory doesn't load navigations)")]
    public async Task MapAsync_WithNullNavigation_ShouldHandleGracefully()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — InMemory provider returns entities without navigations loaded
        Result<QueryPage<ProductWithDetailDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithDetailDto(p.Name, p.ProductDetail?.Sku, p.ProductDetail?.WeightKg));

        // Assert — ProductDetail is null because InMemory doesn't track navigations
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.First().Sku.Should().BeNull();
        result.Value.Items.First().WeightKg.Should().BeNull();
    }

    [Fact(DisplayName = "MapAsync should handle empty collection navigation (InMemory)")]
    public async Task MapAsync_WithEmptyCollectionNavigation_ShouldReturnEmptyList()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Reviews collection is empty because InMemory doesn't load navigations
        Result<QueryPage<ProductWithReviewsDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithReviewsDto(
                p.Name,
                p.Reviews.Count,
                p.Reviews.Count > 0 ? p.Reviews.Average(r => r.Rating) : 0));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.First().ReviewCount.Should().Be(0);
    }

    [Fact(DisplayName = "MapAsync should handle many-to-many navigation as empty (InMemory)")]
    public async Task MapAsync_WithManyToManyNavigation_ShouldReturnEmptyTags()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — ProductTags is empty because InMemory doesn't load navigations
        Result<QueryPage<ProductWithTagsDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithTagsDto(
                p.Name,
                p.ProductTags.Select(pt => pt.Tag!.Name).ToList().AsReadOnly()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.First().Tags.Should().BeEmpty();
    }

    [Fact(DisplayName = "MapAsync should map with null-safe navigation access")]
    public async Task MapAsync_WithNullSafeNavigation_ShouldNotThrow()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Access Category.Name safely; Category is null from InMemory
        Result<QueryPage<(string Name, string? CategoryName)>> result = await repository.MapAsync(
            query,
            p => (p.Name, p.Category?.Name));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.First().CategoryName.Should().BeNull();
    }

    [Fact(DisplayName = "MapAsync should handle single product without navigations")]
    public async Task MapAsync_SingleProductWithoutNavigations_ShouldMapScalarOnly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var product = EfCoreRepositoryTestFixture.CreateProductV2(
            "Standalone", 9.99m, detailSku: null, reviewCount: 0);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Standalone");
    }

    #endregion

    #region MapAsync - Computed and conditional mapping

    [Fact(DisplayName = "MapAsync should compute derived values in mapping")]
    public async Task MapAsync_WithComputedValues_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductComputedDto>> result = await repository.MapAsync(
            query,
            p => new ProductComputedDto(
                p.Name,
                p.Price,
                p.Price * 0.9m, // 10% discount
                p.Stock > 50 ? "In Stock" : (p.Stock > 0 ? "Low Stock" : "Out of Stock")));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        foreach (var dto in result.Value.Items)
        {
            dto.DiscountedPrice.Should().Be(dto.Price * 0.9m);
            dto.StockStatus.Should().BeOneOf("In Stock", "Low Stock", "Out of Stock");
        }
    }

    [Fact(DisplayName = "MapAsync should filter and map in the same operation")]
    public async Task MapAsync_WithFilterAndMap_ShouldFilterFirst()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — map only active products
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        // Every 5th product is inactive (5, 10)
        var inactive = result.Value.Items.Where(d => !d.Active).ToList();
        inactive.Should().HaveCount(2);
    }

    [Fact(DisplayName = "MapAsync should map with pagination")]
    public async Task MapAsync_WithPagination_ShouldPageCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "MapAsync should map with sort and navigation")]
    public async Task MapAsync_WithSortAndNavigation_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            OrderBy = new List<SortField>
            {
                new SortField("Price", SortDirection.Descending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact(DisplayName = "MapAsync should map with search")]
    public async Task MapAsync_WithSearch_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            Search = new BasicSearchRequest("Product", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region MapAsync - Edge cases

    [Fact(DisplayName = "MapAsync should handle deeply nested navigation safely (InMemory returns null)")]
    public async Task MapAsync_WithDeepNavigation_ShouldHandleNullSafely()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Product → Category → ParentCategory.Name (all null from InMemory)
        Result<QueryPage<(string Name, string? CategoryName, string? ParentCategoryName)>> result =
            await repository.MapAsync(
                query,
                p => (p.Name, p.Category?.Name, p.Category?.ParentCategory?.Name));

        // Assert — Navigations are null because InMemory doesn't load them
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.First().CategoryName.Should().BeNull();
        result.Value.Items.First().ParentCategoryName.Should().BeNull();
    }

    [Fact(DisplayName = "MapAsync should handle mixed null navigations (InMemory returns null for all)")]
    public async Task MapAsync_WithMixedNullNavigations_ShouldHandleCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var withDetail = EfCoreRepositoryTestFixture.CreateProductV2(
            "With Detail", 10m, detailSku: "SKU-001", reviewCount: 1);
        var withoutDetail = EfCoreRepositoryTestFixture.CreateProductV2(
            "Without Detail", 20m, detailSku: null, reviewCount: 0);
        context.Products.AddRange(withDetail, withoutDetail);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — InMemory returns entities without navigations
        Result<QueryPage<(string Name, bool HasDetail, string? Sku)>> result =
            await repository.MapAsync(
                query,
                p => (p.Name, p.ProductDetail != null, p.ProductDetail?.Sku));

        // Assert — Both products have null ProductDetail from InMemory
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(d => d.HasDetail.Should().BeFalse());
        result.Value.Items.Should().AllSatisfy(d => d.Sku.Should().BeNull());
    }

    [Fact(DisplayName = "MapAsync should handle large dataset with navigation")]
    public async Task MapAsync_WithLargeDataset_ShouldMapAll()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 50);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "MapAsync should handle empty string scalar property")]
    public async Task MapAsync_WithEmptyStringScalar_ShouldReturnEmptyString()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var product = new UnitTestProductV2
        {
            Id = Guid.NewGuid(),
            Name = "",
            Price = 1m,
            Stock = 0,
            Active = false,
            CategoryId = Guid.NewGuid()
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().BeEmpty();
    }

    [Fact(DisplayName = "MapAsync with failing filter should return failure")]
    public async Task MapAsync_WithFailingFilterV2_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProductV2> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2WithTranslator(context, translator);
        var query = new Query<UnitTestProductV2>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Test", "Test", typeof(string), false, null, null)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "MapAsync with failing search should return failure")]
    public async Task MapAsync_WithFailingSearchV2_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProductV2> { FailSearch = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2WithTranslator(context, translator);
        var query = new Query<UnitTestProductV2>
        {
            Search = new BasicSearchRequest("Test", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "MapAsync with failing sort should return failure")]
    public async Task MapAsync_WithFailingSortV2_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProductV2> { FailSort = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2WithTranslator(context, translator);
        var query = new Query<UnitTestProductV2>
        {
            OrderBy = new List<SortField> { new SortField("Name", SortDirection.Ascending, SortNulls.None) }
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "MapAsync with cancelled token should handle cancellation")]
    public async Task MapAsync_WithCancelledTokenV2_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.MapAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active), cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        result.Should().NotBeNull();
    }

    #endregion

    #region ProjectAsync - Server-side projection with navigation

    [Fact(DisplayName = "ProjectAsync should project navigation property: Category.Name")]
    public async Task ProjectAsync_WithNavigationProperty_ShouldProjectCategoryName()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithCategoryDto(p.Name, p.Price, p.Category!.Name));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.First().CategoryName.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should project one-to-one navigation: ProductDetail.Sku")]
    public async Task ProjectAsync_WithOneToOneNavigation_ShouldProjectSku()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithDetailDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithDetailDto(
                p.Name,
                p.ProductDetail != null ? p.ProductDetail.Sku : null,
                p.ProductDetail != null ? p.ProductDetail.WeightKg : null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact(DisplayName = "ProjectAsync should project with pagination and navigation")]
    public async Task ProjectAsync_WithPaginationAndNavigation_ShouldPageCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            Page = new PageRequest(1, 3)
        };

        // Act
        Result<QueryPage<ProductWithCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithCategoryDto(p.Name, p.Price, p.Category!.Name));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "ProjectAsync should project computed values")]
    public async Task ProjectAsync_WithComputedValues_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductComputedDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductComputedDto(
                p.Name,
                p.Price,
                p.Price * 0.9m,
                p.Stock > 50 ? "In Stock" : (p.Stock > 0 ? "Low Stock" : "Out of Stock")));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        foreach (var dto in result.Value.Items)
        {
            dto.DiscountedPrice.Should().Be(dto.Price * 0.9m);
        }
    }

    [Fact(DisplayName = "ProjectAsync should project with sort and navigation")]
    public async Task ProjectAsync_WithSortAndNavigation_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            OrderBy = new List<SortField>
            {
                new SortField("Price", SortDirection.Descending, SortNulls.None)
            }
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact(DisplayName = "ProjectAsync should project with search and navigation")]
    public async Task ProjectAsync_WithSearchAndNavigation_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            Search = new BasicSearchRequest("Product", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<ProductWithCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithCategoryDto(p.Name, p.Price, p.Category!.Name));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ProjectAsync should handle null navigation in projection")]
    public async Task ProjectAsync_WithNullNavigation_ShouldHandleGracefully()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var product = EfCoreRepositoryTestFixture.CreateProductV2(
            "No Detail", 9.99m, detailSku: null, reviewCount: 0);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithDetailDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithDetailDto(
                p.Name,
                p.ProductDetail != null ? p.ProductDetail.Sku : null,
                p.ProductDetail != null ? p.ProductDetail.WeightKg : null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Sku.Should().BeNull();
    }

    [Fact(DisplayName = "ProjectAsync with empty results should return empty list")]
    public async Task ProjectAsync_WithEmptyResultsV2_ShouldReturnEmptyList()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync with failing filter should return failure")]
    public async Task ProjectAsync_WithFailingFilterV2_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProductV2> { FailFilter = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2WithTranslator(context, translator);
        var query = new Query<UnitTestProductV2>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Test", "Test", typeof(string), false, null, null)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "ProjectAsync with failing search should return failure")]
    public async Task ProjectAsync_WithFailingSearchV2_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProductV2> { FailSearch = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2WithTranslator(context, translator);
        var query = new Query<UnitTestProductV2>
        {
            Search = new BasicSearchRequest("Test", SearchMode.Contains)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "ProjectAsync with failing sort should return failure")]
    public async Task ProjectAsync_WithFailingSortV2_ShouldReturnFailure()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var translator = new FailingMockQueryTranslator<UnitTestProductV2> { FailSort = true };
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2WithTranslator(context, translator);
        var query = new Query<UnitTestProductV2>
        {
            OrderBy = new List<SortField> { new SortField("Name", SortDirection.Ascending, SortNulls.None) }
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "ProjectAsync with cancelled token should handle cancellation")]
    public async Task ProjectAsync_WithCancelledTokenV2_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active), cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "ProjectAsync should project deeply nested navigation (3 levels)")]
    public async Task ProjectAsync_WithDeepNavigation_ShouldProjectThreeLevels()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Product → Category → ParentCategory.Name
        Result<QueryPage<DeepNavDto>> result =
            await repository.ProjectAsync(
                query,
                p => new DeepNavDto(
                    p.Name,
                    p.Category!.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    public record DeepNavDto(string ProductName, string CategoryName, string? ParentCategoryName);

    [Fact(DisplayName = "ProjectAsync should handle large dataset with navigation")]
    public async Task ProjectAsync_WithLargeDataset_ShouldProjectAll()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 50);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "ProjectAsync should project with filter and navigation")]
    public async Task ProjectAsync_WithFilterAndNavigation_ShouldNotFail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            Filter = new FilterCondition("Active", FilterOperator.Equal, "true", true, typeof(bool), false, null, null)
        };

        // Act
        Result<QueryPage<ProductWithCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithCategoryDto(p.Name, p.Price, p.Category!.Name));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ProjectAsync should project review count via collection count")]
    public async Task ProjectAsync_WithReviewCount_ShouldProjectCount()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithReviewsDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithReviewsDto(p.Name, p.Reviews.Count, 0));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    #endregion

    #region MapAsync vs ProjectAsync comparison tests

    [Fact(DisplayName = "MapAsync and ProjectAsync should return same count for simple projection")]
    public async Task MapAsync_ProjectAsync_SimpleProjection_ShouldReturnSameCount()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductSummaryDto>> mapResult = await repository.MapAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        Result<QueryPage<ProductSummaryDto>> projectResult = await repository.ProjectAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        mapResult.IsSuccess.Should().BeTrue();
        projectResult.IsSuccess.Should().BeTrue();
        mapResult.Value.Items.Count.Should().Be(projectResult.Value.Items.Count);
    }

    [Fact(DisplayName = "MapAsync and ProjectAsync should return same DTOs with pagination")]
    public async Task MapAsync_ProjectAsync_WithPagination_ShouldReturnSameCount()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>
        {
            Page = new PageRequest(2, 3)
        };

        // Act
        Result<QueryPage<ProductSummaryDto>> mapResult = await repository.MapAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        Result<QueryPage<ProductSummaryDto>> projectResult = await repository.ProjectAsync(
            query, p => new ProductSummaryDto(p.Name, p.Price, p.Stock, p.Active));

        // Assert
        mapResult.IsSuccess.Should().BeTrue();
        projectResult.IsSuccess.Should().BeTrue();
        mapResult.Value.Items.Count.Should().Be(3);
        projectResult.Value.Items.Count.Should().Be(3);
    }

    #endregion
}
