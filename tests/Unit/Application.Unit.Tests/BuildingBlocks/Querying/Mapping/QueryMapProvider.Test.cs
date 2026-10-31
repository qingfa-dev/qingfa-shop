using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Mapping;

public sealed class QueryMapProviderTest
{
    private sealed class Entity
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private sealed class OtherEntity
    {
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>Minimal mock that returns predetermined results.</summary>
    private sealed class MockProvider : IQueryMapProvider
    {
        private readonly Dictionary<Type, object> _maps = new();

        public void Add<T>(QueryMap<T> map) where T : class => _maps[typeof(T)] = map;

        public bool TryGet<T>(out QueryMap<T>? map) where T : class
        {
            if (_maps.TryGetValue(typeof(T), out object? value))
            {
                map = value as QueryMap<T>;
                return map is not null;
            }
            map = null;
            return false;
        }

        public bool IsRegistered<T>() where T : class => _maps.ContainsKey(typeof(T));
    }

    private static QueryMap<Entity> EntityMap() =>
        Build(
            QueryMapBuilder<Entity>.Create()
                .Map("name", x => x.Name, b => b.Searchable())
                .Map("score", x => x.Score));

    private static QueryMap<OtherEntity> OtherEntityMap() =>
        Build(QueryMapBuilder<OtherEntity>.Create().Map("label", x => x.Label));

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    #region QueryMapRegistry as IQueryMapProvider

    [Fact(DisplayName = "Registry should implement IQueryMapProvider")]
    public void Registry_ShouldImplementIQueryMapProvider()
    {
        var registry = new QueryMapRegistry();
        registry.Should().BeAssignableTo<IQueryMapProvider>();
    }

    [Fact(DisplayName = "IQueryMapProvider.TryGet should return the registered map")]
    public void TryGet_ShouldReturnRegisteredMap()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        bool found = provider.TryGet<Entity>(out QueryMap<Entity>? map);

        found.Should().BeTrue();
        map.Should().NotBeNull();
    }

    [Fact(DisplayName = "IQueryMapProvider.TryGet should return false for unregistered type")]
    public void TryGet_ShouldReturnFalseForUnregisteredType()
    {
        IQueryMapProvider provider = new QueryMapRegistry();

        bool found = provider.TryGet<Entity>(out QueryMap<Entity>? map);

        found.Should().BeFalse();
        map.Should().BeNull();
    }

    [Fact(DisplayName = "IQueryMapProvider.IsRegistered should return true after registration")]
    public void IsRegistered_ShouldReturnTrueAfterRegistration()
    {
        IQueryMapProvider provider = new QueryMapRegistry();

        provider.IsRegistered<Entity>().Should().BeFalse();

        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        provider.IsRegistered<Entity>().Should().BeTrue();
    }

    [Fact(DisplayName = "IQueryMapProvider.IsRegistered should return false for unregistered type")]
    public void IsRegistered_ShouldReturnFalseForUnregisteredType()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        provider.IsRegistered<Entity>().Should().BeTrue();
        provider.IsRegistered<OtherEntity>().Should().BeFalse();
    }

    [Fact(DisplayName = "IQueryMapProvider.TryGet should return distinct maps per type")]
    public void TryGet_ShouldReturnDistinctMapsPerType()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        QueryMap<Entity> first = EntityMap();
        QueryMap<OtherEntity> second = OtherEntityMap();

        ((QueryMapRegistry)provider).Register(first).IsSuccess.Should().BeTrue();
        ((QueryMapRegistry)provider).Register(second).IsSuccess.Should().BeTrue();

        provider.TryGet<Entity>(out QueryMap<Entity>? entityMap).Should().BeTrue();
        provider.TryGet<OtherEntity>(out QueryMap<OtherEntity>? otherMap).Should().BeTrue();

        entityMap.Should().BeSameAs(first);
        otherMap.Should().BeSameAs(second);
    }

    #endregion

    #region Mock Provider

    [Fact(DisplayName = "Mock provider TryGet should return registered map")]
    public void MockProvider_TryGet_ShouldReturnRegisteredMap()
    {
        var provider = new MockProvider();
        QueryMap<Entity> map = EntityMap();
        provider.Add(map);

        bool found = provider.TryGet<Entity>(out QueryMap<Entity>? resolved);

        found.Should().BeTrue();
        resolved.Should().BeSameAs(map);
    }

    [Fact(DisplayName = "Mock provider TryGet should return false for unregistered type")]
    public void MockProvider_TryGet_ShouldReturnFalseForUnregisteredType()
    {
        var provider = new MockProvider();

        bool found = provider.TryGet<Entity>(out QueryMap<Entity>? map);

        found.Should().BeFalse();
        map.Should().BeNull();
    }

    [Fact(DisplayName = "Mock provider IsRegistered should track registrations")]
    public void MockProvider_IsRegistered_ShouldTrackRegistrations()
    {
        var provider = new MockProvider();

        provider.IsRegistered<Entity>().Should().BeFalse();
        provider.Add(EntityMap());
        provider.IsRegistered<Entity>().Should().BeTrue();
        provider.IsRegistered<OtherEntity>().Should().BeFalse();
    }

    #endregion

    #region QueryParser integration with IQueryMapProvider

    [Fact(DisplayName = "QueryParser should work with IQueryMapProvider interface")]
    public void QueryParser_ShouldWorkWithIQueryMapProvider()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters { OrderBy = "score desc" },
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "QueryParser should fail when IQueryMapProvider has no map")]
    public void QueryParser_ShouldFailWhenProviderHasNoMap()
    {
        IQueryMapProvider provider = new QueryMapRegistry();

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters { Filter = "name==x" },
            provider);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Map.NotRegistered");
    }

    [Fact(DisplayName = "QueryParser should work with mock provider")]
    public void QueryParser_ShouldWorkWithMockProvider()
    {
        var provider = new MockProvider();
        provider.Add(EntityMap());

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters { OrderBy = "name asc" },
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "QueryParser should fail with mock provider when map missing")]
    public void QueryParser_ShouldFailWithMockProviderWhenMapMissing()
    {
        var provider = new MockProvider();

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters(),
            provider);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Map.NotRegistered");
    }

    [Fact(DisplayName = "QueryParser should support filter parsing via IQueryMapProvider")]
    public void QueryParser_ShouldSupportFilterParsingViaProvider()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters { Filter = "score>10" },
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().NotBeNull();
    }

    [Fact(DisplayName = "QueryParser should support search parsing via IQueryMapProvider")]
    public void QueryParser_ShouldSupportSearchParsingViaProvider()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters { Search = "hello" },
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
    }

    [Fact(DisplayName = "QueryParser should support pagination via IQueryMapProvider")]
    public void QueryParser_ShouldSupportPaginationViaProvider()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        Result<Query<Entity>> result = QueryParser.Parse<Entity>(
            new QueryParameters { Page = 2, PageSize = 10 },
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().NotBeNull();
        result.Value.Page!.Page.Should().Be(2);
        result.Value.Page.PageSize.Should().Be(10);
    }

    #endregion

    #region Multiple entity types

    [Fact(DisplayName = "Provider should handle multiple entity types")]
    public void Provider_ShouldHandleMultipleEntityTypes()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();
        ((QueryMapRegistry)provider).Register(OtherEntityMap()).IsSuccess.Should().BeTrue();

        // Entity map
        Result<Query<Entity>> entityResult = QueryParser.Parse<Entity>(
            new QueryParameters { OrderBy = "score desc" },
            provider);
        entityResult.IsSuccess.Should().BeTrue();

        // OtherEntity map
        Result<Query<OtherEntity>> otherResult = QueryParser.Parse<OtherEntity>(
            new QueryParameters { OrderBy = "label asc" },
            provider);
        otherResult.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Provider should not leak maps between types")]
    public void Provider_ShouldNotLeakMapsBetweenTypes()
    {
        IQueryMapProvider provider = new QueryMapRegistry();
        ((QueryMapRegistry)provider).Register(EntityMap()).IsSuccess.Should().BeTrue();

        // Entity should resolve
        Result<Query<Entity>> entityResult = QueryParser.Parse<Entity>(
            new QueryParameters(),
            provider);
        entityResult.IsSuccess.Should().BeTrue();

        // OtherEntity should fail
        Result<Query<OtherEntity>> otherResult = QueryParser.Parse<OtherEntity>(
            new QueryParameters(),
            provider);
        otherResult.IsFailure.Should().BeTrue();
    }

    #endregion
}
