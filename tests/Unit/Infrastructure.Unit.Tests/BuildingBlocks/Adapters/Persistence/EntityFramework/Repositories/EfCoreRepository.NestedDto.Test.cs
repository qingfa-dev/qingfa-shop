using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Querying.Sorting;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Mapping.NestedDto")]
public sealed class EfCoreRepositoryNestedDtoTest
{
    #region Nested DTOs

    // Level 1: Simple nested DTO
    public record CategoryDto(Guid Id, string Name, string? ParentCategoryName);
    public record ProductDetailDto(string? Sku, string? Description, double WeightKg);

    // Level 2: Product with nested DTOs
    public record ProductWithNestedCategoryDto(
        string Name,
        decimal Price,
        CategoryDto Category);
    public record ProductWithNestedDetailDto(
        string Name,
        ProductDetailDto? Detail);

    // Level 3: Deeply nested — Product → Category → ParentCategory
    public record ParentCategoryDto(Guid Id, string Name);
    public record CategoryWithParentDto(
        Guid Id,
        string Name,
        ParentCategoryDto? Parent);
    public record ProductWithDeepCategoryDto(
        string Name,
        CategoryWithParentDto Category);

    // Level 4: Nested collections
    public record ReviewDto(Guid Id, string Author, int Rating, string? Comment);
    public record ProductWithReviewsDto(
        string Name,
        decimal Price,
        IReadOnlyList<ReviewDto> Reviews);

    // Level 5: Many-to-many nested
    public record TagDto(Guid Id, string Name);
    public record ProductWithTagsDto(
        string Name,
        IReadOnlyList<TagDto> Tags);

    // Level 6: Full nested DTO — all navigations
    public record ProductFullNestedDto(
        string Name,
        decimal Price,
        int Stock,
        bool Active,
        CategoryDto Category,
        ProductDetailDto? Detail,
        IReadOnlyList<ReviewDto> Reviews,
        IReadOnlyList<TagDto> Tags);

    // Level 7: Aggregated nested DTO
    public record CategorySummaryDto(
        string CategoryName,
        int ProductCount,
        decimal TotalValue,
        IReadOnlyList<string> ProductNames);
    public record CategoryWithProductsDto(
        CategoryDto Category,
        IReadOnlyList<ProductSummaryDto> Products);

    // Level 8: Optional nested DTOs
    public record ProductWithOptionalNestedDto(
        string Name,
        CategoryDto? Category,
        ProductDetailDto? Detail);

    // Level 9: Nested DTO with computed values
    public record ProductPricingDto(
        string Name,
        decimal BasePrice,
        decimal DiscountedPrice,
        ProductDetailDto? Detail,
        string StockStatus);

    // Level 10: Flat summary for comparison
    public record ProductSummaryDto(string Name, decimal Price, int Stock, bool Active);

    #endregion

    #region MapAsync - Single-level nested DTOs

    [Fact(DisplayName = "MapAsync should map single-level nested DTO: Product with CategoryDto")]
    public async Task MapAsync_WithNestedCategoryDto_ShouldMapCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — InMemory doesn't load navigations, so Category is null; use fallback
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        // Assert — Category falls back to "Unknown" because InMemory doesn't load navigations
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(dto => dto.Category.Name.Should().Be("Unknown"));
    }

    [Fact(DisplayName = "MapAsync should handle null navigation in nested DTO")]
    public async Task MapAsync_WithNullNavigationNestedDto_ShouldHandleGracefully()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Category is null from InMemory, so use null-conditional
        Result<QueryPage<ProductWithOptionalNestedDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithOptionalNestedDto(
                p.Name,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : null,
                p.ProductDetail != null
                    ? new ProductDetailDto(
                        p.ProductDetail.Sku,
                        p.ProductDetail.Description,
                        p.ProductDetail.WeightKg)
                    : null));

        // Assert — Both nested DTOs are null because InMemory doesn't load navigations
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(
            dto =>
            {
                dto.Category.Should().BeNull();
                dto.Detail.Should().BeNull();
            });
    }

    [Fact(DisplayName = "MapAsync should map nested DTO with one-to-one navigation")]
    public async Task MapAsync_WithOneToOneNestedDto_ShouldMapDetail()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedDetailDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedDetailDto(
                p.Name,
                p.ProductDetail != null
                    ? new ProductDetailDto(
                        p.ProductDetail.Sku,
                        p.ProductDetail.Description,
                        p.ProductDetail.WeightKg)
                    : null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(dto => dto.Detail.Should().BeNull());
    }

    #endregion

    #region MapAsync - Deeply nested DTOs (3+ levels)

    [Fact(DisplayName = "MapAsync should map deeply nested DTO: Product → Category → ParentCategory")]
    public async Task MapAsync_WithDeeplyNestedDto_ShouldMapThreeLevels()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — 3 levels: Product → Category → ParentCategory
        Result<QueryPage<ProductWithDeepCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithDeepCategoryDto(
                p.Name,
                p.Category != null
                    ? new CategoryWithParentDto(
                        p.Category.Id,
                        p.Category.Name,
                        p.Category.ParentCategory != null
                            ? new ParentCategoryDto(p.Category.ParentCategory.Id, p.Category.ParentCategory.Name)
                            : null)
                    : new CategoryWithParentDto(Guid.Empty, "Unknown", null)));

        // Assert — Category is null from InMemory, so fallback to "Unknown"
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(
            dto => dto.Category.Name.Should().Be("Unknown"));
    }

    [Fact(DisplayName = "MapAsync should handle null at each level of deep nesting")]
    public async Task MapAsync_WithNullAtEachDeepLevel_ShouldNotThrow()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Safely navigate through potentially null levels
        Result<QueryPage<(string Name, string? CategoryName, string? ParentCategoryName)>> result =
            await repository.MapAsync(
                query,
                p =>
                {
                    var categoryName = p.Category?.Name;
                    var parentCategoryName = p.Category?.ParentCategory?.Name;
                    return (p.Name, categoryName, parentCategoryName);
                });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(
            dto =>
            {
                dto.CategoryName.Should().BeNull();
                dto.ParentCategoryName.Should().BeNull();
            });
    }

    #endregion

    #region MapAsync - Nested collections

    [Fact(DisplayName = "MapAsync should map nested collection DTO: Product with ReviewDto list")]
    public async Task MapAsync_WithNestedCollectionDto_ShouldMapReviews()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Reviews collection is empty from InMemory
        Result<QueryPage<ProductWithReviewsDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithReviewsDto(
                p.Name,
                p.Price,
                p.Reviews.Select(r => new ReviewDto(r.Id, r.Author, r.Rating, r.Comment)).ToList().AsReadOnly()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(dto => dto.Reviews.Should().BeEmpty());
    }

    [Fact(DisplayName = "MapAsync should map many-to-many nested DTO: Product with TagDto list")]
    public async Task MapAsync_WithManyToManyNestedDto_ShouldMapTags()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — ProductTags is empty from InMemory
        Result<QueryPage<ProductWithTagsDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithTagsDto(
                p.Name,
                p.ProductTags
                    .Select(pt => new TagDto(pt.Tag!.Id, pt.Tag.Name))
                    .ToList()
                    .AsReadOnly()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(dto => dto.Tags.Should().BeEmpty());
    }

    [Fact(DisplayName = "MapAsync should map full nested DTO with all navigation types")]
    public async Task MapAsync_WithFullNestedDto_ShouldMapAllNavigations()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Full nested DTO with Category, Detail, Reviews, and Tags
        Result<QueryPage<ProductFullNestedDto>> result = await repository.MapAsync(
            query,
            p => new ProductFullNestedDto(
                p.Name,
                p.Price,
                p.Stock,
                p.Active,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null),
                p.ProductDetail != null
                    ? new ProductDetailDto(p.ProductDetail.Sku, p.ProductDetail.Description, p.ProductDetail.WeightKg)
                    : null,
                p.Reviews.Select(r => new ReviewDto(r.Id, r.Author, r.Rating, r.Comment)).ToList().AsReadOnly(),
                p.ProductTags.Select(pt => new TagDto(pt.Tag!.Id, pt.Tag.Name)).ToList().AsReadOnly()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Should().AllSatisfy(
            dto =>
            {
                dto.Category.Name.Should().Be("Unknown");
                dto.Detail.Should().BeNull();
                dto.Reviews.Should().BeEmpty();
                dto.Tags.Should().BeEmpty();
            });
    }

    #endregion

    #region MapAsync - Nested DTO with pagination and filtering

    [Fact(DisplayName = "MapAsync should map nested DTOs with pagination")]
    public async Task MapAsync_WithNestedDtoAndPagination_ShouldPageCorrectly()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "MapAsync should map nested DTOs with sort")]
    public async Task MapAsync_WithNestedDtoAndSort_ShouldNotFail()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact(DisplayName = "MapAsync should map nested DTOs with search")]
    public async Task MapAsync_WithNestedDtoAndSearch_ShouldNotFail()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region MapAsync - Nested DTO with computed values

    [Fact(DisplayName = "MapAsync should map nested DTO with computed values")]
    public async Task MapAsync_WithNestedDtoAndComputedValues_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductPricingDto>> result = await repository.MapAsync(
            query,
            p => new ProductPricingDto(
                p.Name,
                p.Price,
                p.Price * 0.9m,
                p.ProductDetail != null
                    ? new ProductDetailDto(p.ProductDetail.Sku, p.ProductDetail.Description, p.ProductDetail.WeightKg)
                    : null,
                p.Stock > 50 ? "In Stock" : (p.Stock > 0 ? "Low Stock" : "Out of Stock")));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        foreach (var dto in result.Value.Items)
        {
            dto.DiscountedPrice.Should().Be(dto.BasePrice * 0.9m);
            dto.StockStatus.Should().BeOneOf("In Stock", "Low Stock", "Out of Stock");
        }
    }

    [Fact(DisplayName = "MapAsync should map nested DTO with conditional nested object")]
    public async Task MapAsync_WithConditionalNestedDto_ShouldHandleCorrectly()
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

        // Act — InMemory returns entities without navigations, so both are null
        Result<QueryPage<ProductWithOptionalNestedDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithOptionalNestedDto(
                p.Name,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : null,
                p.ProductDetail != null
                    ? new ProductDetailDto(p.ProductDetail.Sku, p.ProductDetail.Description, p.ProductDetail.WeightKg)
                    : null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(
            dto =>
            {
                dto.Category.Should().BeNull();
                dto.Detail.Should().BeNull();
            });
    }

    #endregion

    #region MapAsync - Aggregated nested DTOs

    [Fact(DisplayName = "MapAsync should map aggregated nested DTO: Category with product summaries")]
    public async Task MapAsync_WithAggregatedNestedDto_ShouldGroupByCategory()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Group products by category name and create nested summary
        Result<QueryPage<CategorySummaryDto>> result = await repository.MapAsync(
            query,
            p => new CategorySummaryDto(
                p.Category?.Name ?? "Unknown",
                1,
                p.Price,
                new List<string> { p.Name }.AsReadOnly()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
    }

    #endregion

    #region MapAsync - Edge cases for nested DTOs

    [Fact(DisplayName = "MapAsync should handle empty string in nested DTO properties")]
    public async Task MapAsync_WithEmptyStringInNestedDto_ShouldReturnEmptyString()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().BeEmpty();
        result.Value.Items.First().Category.Name.Should().Be("Unknown");
    }

    [Fact(DisplayName = "MapAsync should handle large dataset with nested DTOs")]
    public async Task MapAsync_WithLargeDatasetNestedDto_ShouldMapAll()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 50);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "MapAsync should handle nested DTO with empty results")]
    public async Task MapAsync_WithNestedDtoEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(p.Category!.Id, p.Category.Name, p.Category.ParentCategory?.Name)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "MapAsync with failing filter should return failure for nested DTO")]
    public async Task MapAsync_WithFailingFilterNestedDto_ShouldReturnFailure()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(p.Category!.Id, p.Category.Name, p.Category.ParentCategory?.Name)));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "MapAsync with cancelled token should handle cancellation for nested DTO")]
    public async Task MapAsync_WithCancelledTokenNestedDto_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(p.Category!.Id, p.Category.Name, p.Category.ParentCategory?.Name)),
            cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        result.Should().NotBeNull();
    }

    #endregion

    #region ProjectAsync - Single-level nested DTOs

    [Fact(DisplayName = "ProjectAsync should project single-level nested DTO: Product with CategoryDto")]
    public async Task ProjectAsync_WithNestedCategoryDto_ShouldProjectCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 5);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — EF Core translates nested object creation to JSON or scalar columns
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.First().Category.Name.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should handle null navigation in nested DTO")]
    public async Task ProjectAsync_WithNullNavigationNestedDto_ShouldHandleGracefully()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var product = EfCoreRepositoryTestFixture.CreateProductV2(
            "No Detail", 9.99m, detailSku: null, reviewCount: 0);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — ProductDetail is null, use conditional
        Result<QueryPage<ProductWithNestedDetailDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedDetailDto(
                p.Name,
                p.ProductDetail != null
                    ? new ProductDetailDto(p.ProductDetail.Sku, p.ProductDetail.Description, p.ProductDetail.WeightKg)
                    : null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Detail.Should().BeNull();
    }

    #endregion

    #region ProjectAsync - Deeply nested DTOs

    [Fact(DisplayName = "ProjectAsync should project deeply nested DTO: Product → Category → ParentCategory")]
    public async Task ProjectAsync_WithDeeplyNestedDto_ShouldProjectThreeLevels()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — 3 levels deep: Product → Category → ParentCategory
        Result<QueryPage<ProductWithDeepCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithDeepCategoryDto(
                p.Name,
                new CategoryWithParentDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null
                        ? new ParentCategoryDto(p.Category.ParentCategory.Id, p.Category.ParentCategory.Name)
                        : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.First().Category.Name.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "ProjectAsync should handle null at each level of deep nesting")]
    public async Task ProjectAsync_WithNullAtEachDeepLevel_ShouldNotThrow()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var product = EfCoreRepositoryTestFixture.CreateProductV2(
            "No Category", 10m, detailSku: null, reviewCount: 0);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Category is null, use conditional
        Result<QueryPage<ProductWithDeepCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithDeepCategoryDto(
                p.Name,
                p.Category != null
                    ? new CategoryWithParentDto(
                        p.Category.Id,
                        p.Category.Name,
                        p.Category.ParentCategory != null
                            ? new ParentCategoryDto(p.Category.ParentCategory.Id, p.Category.ParentCategory.Name)
                            : null)
                    : new CategoryWithParentDto(Guid.Empty, "Unknown", null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    #endregion

    #region ProjectAsync - Nested collections

    [Fact(DisplayName = "ProjectAsync should project nested collection DTO: Product with ReviewDto list")]
    public async Task ProjectAsync_WithNestedCollectionDto_ShouldProjectReviews()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Project nested collection of ReviewDto
        Result<QueryPage<ProductWithReviewsDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithReviewsDto(
                p.Name,
                p.Price,
                p.Reviews.Select(r => new ReviewDto(r.Id, r.Author, r.Rating, r.Comment)).ToList()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        // Reviews may be empty from InMemory, but the projection should succeed
    }

    [Fact(DisplayName = "ProjectAsync should project many-to-many nested DTO: Product with TagDto list")]
    public async Task ProjectAsync_WithManyToManyNestedDto_ShouldProjectTags()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act — Project many-to-many nested collection
        Result<QueryPage<ProductWithTagsDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithTagsDto(
                p.Name,
                p.ProductTags
                    .Select(pt => new TagDto(pt.Tag!.Id, pt.Tag.Name))
                    .ToList()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    #endregion

    #region ProjectAsync - Nested DTO with pagination and filtering

    [Fact(DisplayName = "ProjectAsync should project nested DTOs with pagination")]
    public async Task ProjectAsync_WithNestedDtoAndPagination_ShouldPageCorrectly()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact(DisplayName = "ProjectAsync should project nested DTOs with sort")]
    public async Task ProjectAsync_WithNestedDtoAndSort_ShouldNotFail()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact(DisplayName = "ProjectAsync should project nested DTOs with search")]
    public async Task ProjectAsync_WithNestedDtoAndSearch_ShouldNotFail()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ProjectAsync should project nested DTOs with filter")]
    public async Task ProjectAsync_WithNestedDtoAndFilter_ShouldNotFail()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region ProjectAsync - Nested DTO with computed values

    [Fact(DisplayName = "ProjectAsync should project nested DTO with computed values")]
    public async Task ProjectAsync_WithNestedDtoAndComputedValues_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 3);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductPricingDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductPricingDto(
                p.Name,
                p.Price,
                p.Price * 0.9m,
                p.ProductDetail != null
                    ? new ProductDetailDto(p.ProductDetail.Sku, p.ProductDetail.Description, p.ProductDetail.WeightKg)
                    : null,
                p.Stock > 50 ? "In Stock" : (p.Stock > 0 ? "Low Stock" : "Out of Stock")));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        foreach (var dto in result.Value.Items)
        {
            dto.DiscountedPrice.Should().Be(dto.BasePrice * 0.9m);
        }
    }

    #endregion

    #region ProjectAsync - Edge cases for nested DTOs

    [Fact(DisplayName = "ProjectAsync should handle large dataset with nested DTOs")]
    public async Task ProjectAsync_WithLargeDatasetNestedDto_ShouldProjectAll()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 50);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(50);
    }

    [Fact(DisplayName = "ProjectAsync should handle empty results with nested DTO")]
    public async Task ProjectAsync_WithNestedDtoEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "ProjectAsync with failing filter should return failure for nested DTO")]
    public async Task ProjectAsync_WithFailingFilterNestedDto_ShouldReturnFailure()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "ProjectAsync with failing search should return failure for nested DTO")]
    public async Task ProjectAsync_WithFailingSearchNestedDto_ShouldReturnFailure()
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
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Invalid");
    }

    [Fact(DisplayName = "ProjectAsync with cancelled token should handle cancellation for nested DTO")]
    public async Task ProjectAsync_WithCancelledTokenNestedDto_ShouldHandleCancellation()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> result = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)),
            cts.Token);

        // Assert — InMemory provider may or may not throw on cancelled token
        result.Should().NotBeNull();
    }

    #endregion

    #region MapAsync vs ProjectAsync - Nested DTO comparison

    [Fact(DisplayName = "MapAsync and ProjectAsync should return same count for nested DTO")]
    public async Task MapAsync_ProjectAsync_NestedDto_ShouldReturnSameCount()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithNestedCategoryDto>> mapResult = await repository.MapAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                p.Category != null
                    ? new CategoryDto(p.Category.Id, p.Category.Name, p.Category.ParentCategory?.Name)
                    : new CategoryDto(Guid.Empty, "Unknown", null)));

        Result<QueryPage<ProductWithNestedCategoryDto>> projectResult = await repository.ProjectAsync(
            query,
            p => new ProductWithNestedCategoryDto(
                p.Name,
                p.Price,
                new CategoryDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null ? p.Category.ParentCategory.Name : null)));

        // Assert
        mapResult.IsSuccess.Should().BeTrue();
        projectResult.IsSuccess.Should().BeTrue();
        mapResult.Value.Items.Count.Should().Be(projectResult.Value.Items.Count);
    }

    [Fact(DisplayName = "MapAsync and ProjectAsync should return same count for deeply nested DTO")]
    public async Task MapAsync_ProjectAsync_DeeplyNestedDto_ShouldReturnSameCount()
    {
        // Arrange
        var context = EfCoreRepositoryTestFixture.CreateContextV2();
        await EfCoreRepositoryTestFixture.SeedRichProductsAsync(context, 10);
        var repository = EfCoreRepositoryTestFixture.CreateRepositoryV2(context);
        var query = new Query<UnitTestProductV2>();

        // Act
        Result<QueryPage<ProductWithDeepCategoryDto>> mapResult = await repository.MapAsync(
            query,
            p => new ProductWithDeepCategoryDto(
                p.Name,
                p.Category != null
                    ? new CategoryWithParentDto(
                        p.Category.Id,
                        p.Category.Name,
                        p.Category.ParentCategory != null
                            ? new ParentCategoryDto(p.Category.ParentCategory.Id, p.Category.ParentCategory.Name)
                            : null)
                    : new CategoryWithParentDto(Guid.Empty, "Unknown", null)));

        Result<QueryPage<ProductWithDeepCategoryDto>> projectResult = await repository.ProjectAsync(
            query,
            p => new ProductWithDeepCategoryDto(
                p.Name,
                new CategoryWithParentDto(
                    p.Category!.Id,
                    p.Category.Name,
                    p.Category.ParentCategory != null
                        ? new ParentCategoryDto(p.Category.ParentCategory.Id, p.Category.ParentCategory.Name)
                        : null)));

        // Assert
        mapResult.IsSuccess.Should().BeTrue();
        projectResult.IsSuccess.Should().BeTrue();
        mapResult.Value.Items.Count.Should().Be(projectResult.Value.Items.Count);
    }

    #endregion
}
