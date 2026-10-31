using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Querying.Sorting;

using Core.BuildingBlocks.Functional.Results;

namespace Infrastructure.Integration.Tests.BuildingBlocks.Persistence;

[Collection(EfCoreCollection.Name)]
public sealed class EfCoreRepositoryIntegrationTest : IAsyncLifetime
{
    private readonly EfCoreFixture _fixture;

    public EfCoreRepositoryIntegrationTest(EfCoreFixture fixture)
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
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var product = CreateProduct("Widget A", 9.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result<TestProduct> result = await _fixture.Repository.GetByIdAsync(product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Widget A");
        result.Value.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEntity_ReturnsNotFound()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act
        Result<TestProduct> result = await _fixture.Repository.GetByIdAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task FindByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var product = CreateProduct("Widget B", 19.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result<Maybe<TestProduct>> result = await _fixture.Repository.FindByIdAsync(product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Name.Should().Be("Widget B");
    }

    [Fact]
    public async Task FindByIdAsync_NonExistentEntity_ReturnsEmpty()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act
        Result<Maybe<TestProduct>> result = await _fixture.Repository.FindByIdAsync(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ExistingEntity_ReturnsTrue()
    {
        // Arrange
        var product = CreateProduct("Widget C", 29.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _fixture.Repository.ExistsAsync(product.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentEntity_ReturnsFalse()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act
        Result<bool> result = await _fixture.Repository.ExistsAsync(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_NewEntity_StagesForSave()
    {
        // Arrange
        var product = CreateProduct("Widget D", 39.99m);

        // Act
        Result addResult = await _fixture.Repository.AddAsync(product);
        Result saveResult = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();

        Result<TestProduct> getResult = await _fixture.Repository.GetByIdAsync(product.Id);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Name.Should().Be("Widget D");
    }

    [Fact]
    public async Task AddRangeAsync_MultipleEntities_StagesForSave()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Widget E1", 10m),
            CreateProduct("Widget E2", 20m),
            CreateProduct("Widget E3", 30m)
        };

        // Act
        Result addResult = await _fixture.Repository.AddRangeAsync(products);
        Result saveResult = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();

        foreach (var product in products)
        {
            Result<bool> exists = await _fixture.Repository.ExistsAsync(product.Id);
            exists.IsSuccess.Should().BeTrue();
            exists.Value.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesValues()
    {
        // Arrange
        var product = CreateProduct("Widget F", 49.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        product.Name = "Widget F Updated";
        product.Price = 59.99m;
        Result updateResult = await _fixture.Repository.UpdateAsync(product);
        Result saveResult = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();

        // Refresh from DB
        var refreshed = _fixture.DbContext.Products.Find(product.Id);
        refreshed.Should().NotBeNull();
        refreshed!.Name.Should().Be("Widget F Updated");
        refreshed.Price.Should().Be(59.99m);
    }

    [Fact]
    public async Task RemoveAsync_ExistingEntity_RemovesIt()
    {
        // Arrange
        var product = CreateProduct("Widget G", 59.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result removeResult = await _fixture.Repository.RemoveAsync(product);
        Result saveResult = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();

        Result<bool> exists = await _fixture.Repository.ExistsAsync(product.Id);
        exists.Value.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_ById_RemovesIt()
    {
        // Arrange
        var product = CreateProduct("Widget H", 69.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result removeResult = await _fixture.Repository.RemoveAsync(product.Id);
        Result saveResult = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();

        Result<bool> exists = await _fixture.Repository.ExistsAsync(product.Id);
        exists.Value.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act
        Result result = await _fixture.Repository.RemoveAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task RemoveRangeAsync_MultipleEntities_RemovesThem()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Widget I1", 10m),
            CreateProduct("Widget I2", 20m),
            CreateProduct("Widget I3", 30m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        Result removeResult = await _fixture.Repository.RemoveRangeAsync(products);
        Result saveResult = await _fixture.UnitOfWork.SaveChangesAsync();

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();

        foreach (var product in products)
        {
            Result<bool> exists = await _fixture.Repository.ExistsAsync(product.Id);
            exists.Value.Should().BeFalse();
        }
    }

    [Fact]
    public async Task QueryAsync_WithFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Alpha", 10m),
            CreateProduct("Beta", 20m),
            CreateProduct("Gamma", 30m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Beta", "Beta", typeof(string), true, null, null)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Beta");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ReturnsPage()
    {
        // Arrange
        var products = Enumerable.Range(1, 25)
            .Select(i => CreateProduct($"Product {i}", i))
            .ToList();
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Page = new PageRequest(2, 10)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_WithSorting_ReturnsSorted()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Charlie", 30m),
            CreateProduct("Alpha", 10m),
            CreateProduct("Beta", 20m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            OrderBy = [new SortField("Name", SortDirection.Ascending, SortNulls.None)]
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items[0].Name.Should().Be("Alpha");
        result.Value.Items[1].Name.Should().Be("Beta");
        result.Value.Items[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task QuerySingleAsync_MatchingEntity_ReturnsIt()
    {
        // Arrange
        var product = CreateProduct("Unique", 99.99m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Unique", "Unique", typeof(string), true, null, null)
        };
        Result<TestProduct> result = await _fixture.Repository.QuerySingleAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Unique");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_MatchingEntity_ReturnsIt()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("First", 10m),
            CreateProduct("Second", 20m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            OrderBy = [new SortField("Name", SortDirection.Ascending, SortNulls.None)]
        };
        Result<Maybe<TestProduct>> result = await _fixture.Repository.QueryFirstOrDefaultAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Name.Should().Be("First");
    }

    [Fact]
    public async Task CountAsync_WithFilter_ReturnsCount()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("A", 10m),
            CreateProduct("B", 20m),
            CreateProduct("C", 30m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Filter = new FilterCondition("Price", FilterOperator.GreaterThan, "15", 15m, typeof(decimal), false, null, null)
        };
        Result<int> result = await _fixture.Repository.CountAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task AnyAsync_WithFilter_ReturnsTrue()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Exists", 10m),
            CreateProduct("Other", 20m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "Exists", "Exists", typeof(string), true, null, null)
        };
        Result<bool> result = await _fixture.Repository.AnyAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_NoMatch_ReturnsFalse()
    {
        // Arrange
        var product = CreateProduct("Exists", 10m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "NonExistent", "NonExistent", typeof(string), true, null, null)
        };
        Result<bool> result = await _fixture.Repository.AnyAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_WithSearch_ReturnsMatchingEntities()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Widget A", 10m),
            CreateProduct("Gadget B", 20m),
            CreateProduct("Widget C", 30m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Search = new BasicSearchRequest("Widget", SearchMode.Contains)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(p => p.Name.Contains("Widget"));
    }

    [Fact]
    public async Task QueryAsync_WithDescendingSort_ReturnsSorted()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Alpha", 10m),
            CreateProduct("Beta", 20m),
            CreateProduct("Charlie", 30m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            OrderBy = [new SortField("Name", SortDirection.Descending, SortNulls.None)]
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items[0].Name.Should().Be("Charlie");
        result.Value.Items[2].Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task QueryAsync_WithNestedFilterGroup_ReturnsMatchingEntities()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Alpha", 10m),
            CreateProduct("Beta", 25m),
            CreateProduct("Gamma", 40m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - price between 15 and 35
        var query = new Query<TestProduct>
        {
            Filter = new FilterGroup(FilterLogic.And,
                new FilterNode[] {
                    new FilterCondition("Price", FilterOperator.GreaterThan, "15", 15m, typeof(decimal), false, null, null),
                    new FilterCondition("Price", FilterOperator.LessThan, "35", 35m, typeof(decimal), false, null, null)
                })
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Beta");
    }

    [Fact]
    public async Task QueryAsync_LastPage_ReturnsRemainingItems()
    {
        // Arrange
        var products = Enumerable.Range(1, 23)
            .Select(i => CreateProduct($"Product {i}", i))
            .ToList();
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - page 3 of 10 should have 3 items
        var query = new Query<TestProduct>
        {
            Page = new PageRequest(3, 10)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(23);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_PageBeyondTotal_ReturnsEmpty()
    {
        // Arrange
        var products = Enumerable.Range(1, 5)
            .Select(i => CreateProduct($"Product {i}", i))
            .ToList();
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Page = new PageRequest(10, 10)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task QueryAsync_EmptyResults_ReturnsEmptyPage()
    {
        // Arrange
        var product = CreateProduct("Only", 10m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Filter = new FilterCondition("Name", FilterOperator.Equal, "NonExistent", "NonExistent", typeof(string), true, null, null)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task QueryAsync_SingleItemPage_ReturnsOneItem()
    {
        // Arrange
        var product = CreateProduct("Only", 10m);
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Page = new PageRequest(1, 1)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_NullEntity_ReturnsFailure()
    {
        // Act
        Result result = await _fixture.Repository.AddAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact]
    public async Task AddRangeAsync_EmptyCollection_Succeeds()
    {
        // Act
        Result result = await _fixture.Repository.AddRangeAsync(Array.Empty<TestProduct>());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ReturnsFailure()
    {
        // Act
        Result result = await _fixture.Repository.UpdateAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact]
    public async Task RemoveAsync_NullEntity_ReturnsFailure()
    {
        // Act
        Result result = await _fixture.Repository.RemoveAsync((TestProduct?)null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.First().Code.Should().Contain("Required");
    }

    [Fact]
    public async Task RemoveRangeAsync_EmptyCollection_Succeeds()
    {
        // Act
        Result result = await _fixture.Repository.RemoveRangeAsync(Array.Empty<TestProduct>());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_WithSpecialCharactersInSearch_ShouldSucceed()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Café Latte", 5m),
            CreateProduct("Green Tea", 3m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            Search = new BasicSearchRequest("Café", SearchMode.Contains)
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Café Latte");
    }

    [Fact]
    public async Task QueryAsync_WithMultipleSortFields_ReturnsSorted()
    {
        // Arrange
        var products = new[]
        {
            CreateProduct("Alpha", 30m),
            CreateProduct("Alpha", 10m),
            CreateProduct("Beta", 20m)
        };
        _fixture.DbContext.Products.AddRange(products);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var query = new Query<TestProduct>
        {
            OrderBy = [
                new SortField("Name", SortDirection.Ascending, SortNulls.None),
                new SortField("Price", SortDirection.Descending, SortNulls.None)
            ]
        };
        Result<Application.BuildingBlocks.Querying.Pagination.QueryPage<TestProduct>> result
            = await _fixture.Repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        // Alpha with higher price should come first among Alphas
        result.Value.Items[0].Name.Should().Be("Alpha");
        result.Value.Items[0].Price.Should().Be(30m);
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
