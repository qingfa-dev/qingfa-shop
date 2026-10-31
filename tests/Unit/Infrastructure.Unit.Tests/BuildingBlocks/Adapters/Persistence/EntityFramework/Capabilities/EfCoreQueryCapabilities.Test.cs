using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Persistence.Capabilities;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Sorting;

using Core.BuildingBlocks.Domain.Entities;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

using Microsoft.EntityFrameworkCore;
using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities;

/// <summary>
/// A minimal EF Core context for capability tests.
/// </summary>
public sealed class CapabilityTestDbContext : EfCoreDbContext
{
    public DbSet<CapabilityTestEntity> Entities => Set<CapabilityTestEntity>();

    public CapabilityTestDbContext(DbContextOptions<CapabilityTestDbContext> options)
        : base(options)
    {
    }
}

/// <summary>
/// A minimal entity for capability tests.
/// </summary>
public sealed class CapabilityTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool IsDeleted { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// Mock translator that passes through for capability tests.
/// </summary>
public sealed class CapabilityMockTranslator : IQueryTranslator<CapabilityTestEntity>
{
    public Result<IQueryable<CapabilityTestEntity>> ApplyFilter(
        IQueryable<CapabilityTestEntity> source, FilterNode? filter, QueryMap<CapabilityTestEntity> map)
        => Result<IQueryable<CapabilityTestEntity>>.Success(source);

    public Result<IQueryable<CapabilityTestEntity>> ApplySearch(
        IQueryable<CapabilityTestEntity> source, string? query, QueryMap<CapabilityTestEntity> map)
        => Result<IQueryable<CapabilityTestEntity>>.Success(source);

    public Result<IQueryable<CapabilityTestEntity>> ApplySort(
        IQueryable<CapabilityTestEntity> source, IReadOnlyList<SortField> fields, QueryMap<CapabilityTestEntity> map)
        => Result<IQueryable<CapabilityTestEntity>>.Success(source);

    public Result<IQueryable<CapabilityTestEntity>> ApplyPaging(
        IQueryable<CapabilityTestEntity> source, PageRequest page)
        => Result<IQueryable<CapabilityTestEntity>>.Success(source.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize));

    public Result<TabularPlan> BuildSelectPlan(
        IQueryable<CapabilityTestEntity> source, IReadOnlyList<SelectField> fields, QueryMap<CapabilityTestEntity> map)
        => Result<TabularPlan>.Fail(RepositoryOutcome.Failure.QueryInvalid("Not supported in mock"));

    public Result<TabularResult> BuildTabularClientSide(
        IReadOnlyList<CapabilityTestEntity> entities, Query<CapabilityTestEntity> query, QueryMap<CapabilityTestEntity> map)
        => Result<TabularResult>.Fail(RepositoryOutcome.Failure.QueryInvalid("Not supported in mock"));

    public IQueryable<CapabilityTestEntity> IncludeNavigations(
        IQueryable<CapabilityTestEntity> source, Query<CapabilityTestEntity> query, QueryMap<CapabilityTestEntity> map)
        => source;
}

/// <summary>
/// Minimal query map for capability tests.
/// </summary>
public static class CapabilityTestMap
{
    public static QueryMap<CapabilityTestEntity> Create()
    {
        var result = QueryMapBuilder<CapabilityTestEntity>.Create()
            .Map("Name", e => e.Name, b => b.Searchable().Sortable())
            .Map("Value", e => e.Value, b => b.Sortable())
            .Build();
        return result.Value!;
    }
}

/// <summary>
/// Shared helpers for capability tests.
/// </summary>
public static class CapabilityTestFixture
{
    public static CapabilityTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CapabilityTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CapabilityTestDbContext(options);
    }

    public static EfCoreRepository<CapabilityTestEntity, Guid> CreateRepository(
        CapabilityTestDbContext context,
        IQueryCapabilities capabilities)
    {
        var translator = new CapabilityMockTranslator();
        var map = CapabilityTestMap.Create();
        var registry = new QueryMapRegistry();
        registry.Register(map);
        return new EfCoreRepository<CapabilityTestEntity, Guid>(context, translator, registry, capabilities);
    }
}

#region EfCoreQueryCapabilities Tests

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Capabilities.EfCore")]
public sealed class EfCoreQueryCapabilitiesTest
{
    [Fact(DisplayName = "EfCoreQueryCapabilities should support all shape operations")]
    public void EfCore_ShouldSupportAllShapes()
    {
        var caps = new EfCoreQueryCapabilities();

        caps.SupportsExpressions.Should().BeTrue();
        caps.SupportsGrouping.Should().BeTrue();
        caps.SupportsAggregates.Should().BeTrue();
        caps.SupportsJoins.Should().BeTrue();
        caps.SupportsSubqueries.Should().BeTrue();
        caps.SupportsDistinct.Should().BeTrue();
        caps.SupportsUnion.Should().BeTrue();
        caps.SupportsCaseInsensitive.Should().BeTrue();
        caps.SupportsStreaming.Should().BeTrue();
        caps.SupportsProjection.Should().BeTrue();
        caps.SupportsChangeTracking.Should().BeTrue();
    }

    [Fact(DisplayName = "EfCoreQueryCapabilities should support basic search modes")]
    public void EfCore_ShouldSupportBasicSearchModes()
    {
        var caps = new EfCoreQueryCapabilities();

        ((caps.SearchModes & SearchModeSupport.Contains) != 0).Should().BeTrue();
        ((caps.SearchModes & SearchModeSupport.StartsWith) != 0).Should().BeTrue();
        ((caps.SearchModes & SearchModeSupport.Exact) != 0).Should().BeTrue();
        ((caps.SearchModes & SearchModeSupport.FullText) != 0).Should().BeFalse();
        ((caps.SearchModes & SearchModeSupport.Fuzzy) != 0).Should().BeFalse();
    }

    [Fact(DisplayName = "EfCoreQueryCapabilities should not support ranking or highlighting")]
    public void EfCore_ShouldNotSupportRankingOrHighlighting()
    {
        var caps = new EfCoreQueryCapabilities();

        caps.SupportsRanking.Should().BeFalse();
        caps.SupportsHighlighting.Should().BeFalse();
    }

    [Fact(DisplayName = "EfCoreQueryCapabilities should set search term and field limits")]
    public void EfCore_ShouldSetSearchLimits()
    {
        var caps = new EfCoreQueryCapabilities();

        caps.MaxSearchTerms.Should().Be(QueryCapabilityConstant.Search.MaxTerms);
        caps.MaxSearchFields.Should().Be(QueryCapabilityConstant.Search.MaxFields);
    }

    [Fact(DisplayName = "EfCoreQueryCapabilities should set a reasonable max page size")]
    public void EfCore_ShouldSetMaxPageSize()
    {
        var caps = new EfCoreQueryCapabilities();

        caps.MaxPageSize.Should().Be(10_000);
    }

    [Fact(DisplayName = "EfCoreQueryCapabilities should have no expression depth or order-by limit")]
    public void EfCore_ShouldHaveNoExpressionOrOrderByLimit()
    {
        var caps = new EfCoreQueryCapabilities();

        caps.MaxExpressionDepth.Should().BeNull();
        caps.MaxOrderByClauses.Should().BeNull();
    }
}

#endregion

#region Repository Capability Integration Tests

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence.Capabilities.Repository")]
public sealed class EfCoreRepositoryCapabilitiesTest
{
    /// <summary>Provider that supports nothing — used to test capability guards.</summary>
    private sealed class EmptyCapabilities : IQueryCapabilities
    {
        public bool SupportsExpressions => false;
        public bool SupportsGrouping => false;
        public bool SupportsAggregates => false;
        public bool SupportsJoins => false;
        public bool SupportsSubqueries => false;
        public bool SupportsDistinct => false;
        public bool SupportsUnion => false;
        public bool SupportsCaseInsensitive => false;
        public bool SupportsStreaming => false;
        public bool SupportsProjection => false;
        public bool SupportsChangeTracking => false;
        public SearchModeSupport SearchModes => SearchModeSupport.None;
        public bool SupportsRanking => false;
        public bool SupportsHighlighting => false;
        public int? MaxSearchTerms => null;
        public int? MaxSearchFields => null;
        public int? MaxPageSize => 10;
        public int? MaxExpressionDepth => null;
        public int? MaxOrderByClauses => 2;
    }

    [Fact(DisplayName = "Repository should expose capabilities property")]
    public void Repository_ShouldExposeCapabilities()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EfCoreQueryCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);

        // Assert
        repository.Capabilities.Should().BeSameAs(caps);
    }

    [Fact(DisplayName = "StreamAsync should fail when provider does not support streaming")]
    public async Task StreamAsync_Unsupported_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>();

        // Act
        Result<IAsyncEnumerable<CapabilityTestEntity>> result = await repository.StreamAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "QueryTabularAsync should fail when provider does not support projection")]
    public async Task QueryTabularAsync_Unsupported_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>();

        // Act
        Result<TabularResult> result = await repository.QueryTabularAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "QueryTabularAsync should fail when provider does not support grouping")]
    public async Task QueryTabularAsync_GroupingUnsupported_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            GroupBy = [new GroupByField("Name")]
        };

        // Act
        Result<TabularResult> result = await repository.QueryTabularAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "QueryTabularAsync should fail when provider does not support aggregates")]
    public async Task QueryTabularAsync_AggregatesUnsupported_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "Name", "Count")]
        };

        // Act
        Result<TabularResult> result = await repository.QueryTabularAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "ProjectAsync should fail when provider does not support projection")]
    public async Task ProjectAsync_Unsupported_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>();

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query,
            e => e.Name);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "QueryAsync should fail when page size exceeds provider limit")]
    public async Task QueryAsync_PageSizeExceeded_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities(); // MaxPageSize = 10
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            Page = new PageRequest(1, 100)
        };

        // Act
        Result<QueryPage<CapabilityTestEntity>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.PageSizeExceedsProviderLimit);
    }

    [Fact(DisplayName = "QueryAsync should fail when order-by exceeds provider limit")]
    public async Task QueryAsync_OrderByExceeded_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities(); // MaxOrderByClauses = 2
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            OrderBy = [new SortField("Name", SortDirection.Ascending, SortNulls.None), new SortField("Value", SortDirection.Ascending, SortNulls.None), new SortField("Name", SortDirection.Ascending, SortNulls.None)]
        };

        // Act
        Result<QueryPage<CapabilityTestEntity>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.OrderByClausesExceedProviderLimit);
    }

    [Fact(DisplayName = "MapAsync should fail when page size exceeds provider limit")]
    public async Task MapAsync_PageSizeExceeded_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities(); // MaxPageSize = 10
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            Page = new PageRequest(1, 100)
        };

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query,
            e => e.Name);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.PageSizeExceedsProviderLimit);
    }

    [Fact(DisplayName = "ProjectAsync should fail when page size exceeds provider limit")]
    public async Task ProjectAsync_PageSizeExceeded_ShouldFail()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EmptyCapabilities(); // MaxPageSize = 10
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            Page = new PageRequest(1, 100)
        };

        // Act
        Result<QueryPage<string>> result = await repository.ProjectAsync(
            query,
            e => e.Name);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(QueryCapabilityOutcome.Codes.CapabilityNotSupported);
    }

    [Fact(DisplayName = "QueryAsync should succeed with EfCoreQueryCapabilities")]
    public async Task QueryAsync_WithEfCoreCaps_ShouldSucceed()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EfCoreQueryCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>
        {
            Page = new PageRequest(1, 50)
        };

        // Act
        Result<QueryPage<CapabilityTestEntity>> result = await repository.QueryAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "StreamAsync should succeed with EfCoreQueryCapabilities")]
    public async Task StreamAsync_WithEfCoreCaps_ShouldSucceed()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EfCoreQueryCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>();

        // Act
        Result<IAsyncEnumerable<CapabilityTestEntity>> result = await repository.StreamAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "MapAsync should succeed with EfCoreQueryCapabilities")]
    public async Task MapAsync_WithEfCoreCaps_ShouldSucceed()
    {
        // Arrange
        var context = CapabilityTestFixture.CreateContext();
        var caps = new EfCoreQueryCapabilities();
        var repository = CapabilityTestFixture.CreateRepository(context, caps);
        var query = new Query<CapabilityTestEntity>();

        // Act
        Result<QueryPage<string>> result = await repository.MapAsync(
            query,
            e => e.Name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}

#endregion
