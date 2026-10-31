using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Mapping;


public sealed class QueryMapRegistryTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private sealed class OtherTarget
    {
        public string Name { get; set; } = string.Empty;
    }

    private static QueryMap<Target> Map() =>
        Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name)
                .Map("score", x => x.Score));

    private static QueryMap<OtherTarget> OtherMap() =>
        Build(QueryMapBuilder<OtherTarget>.Create().Map("name", x => x.Name));

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> map = builder.Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Fact(DisplayName = "Register Should Resolve The Same Map")]
    public void Register_ShouldResolveTheSameMap()
    {
        var registry = new QueryMapRegistry();
        QueryMap<Target> map = Map();

        registry.Register(map).IsSuccess.Should().BeTrue();

        registry.TryGet<Target>(out QueryMap<Target>? resolved).Should().BeTrue();
        resolved.Should().BeSameAs(map);
    }

    [Fact(DisplayName = "Register When Type Already Has A Map Should Report Conflict")]
    public void Register_WhenTypeAlreadyHasAMap_ShouldReportConflict()
    {
        var registry = new QueryMapRegistry();
        registry.Register(Map()).IsSuccess.Should().BeTrue();

        Result duplicate = registry.Register(Map());

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Map.AlreadyRegistered");
        duplicate.StatusCode.Should().Be(409);
    }

    [Fact(DisplayName = "Register When Map Is Null Should Throw")]
    public void Register_WhenMapIsNull_ShouldThrow()
    {
        var registry = new QueryMapRegistry();

        Action act = () => registry.Register<Target>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Try Get When Type Is Not Registered Should Return False")]
    public void TryGet_WhenTypeIsNotRegistered_ShouldReturnFalse()
    {
        var registry = new QueryMapRegistry();

        registry.TryGet<Target>(out QueryMap<Target>? map).Should().BeFalse();
        map.Should().BeNull();
    }

    [Fact(DisplayName = "Get When Type Is Not Registered Should Fail")]
    public void Get_WhenTypeIsNotRegistered_ShouldFail()
    {
        var registry = new QueryMapRegistry();

        Result<QueryMap<Target>> result = registry.Get<Target>();

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Map.NotRegistered");
    }

    [Fact(DisplayName = "Contains Should Track Registration")]
    public void Contains_ShouldTrackRegistration()
    {
        var registry = new QueryMapRegistry();

        registry.Contains<Target>().Should().BeFalse();

        registry.Register(Map()).IsSuccess.Should().BeTrue();

        registry.Contains<Target>().Should().BeTrue();
        registry.Contains<OtherTarget>().Should().BeFalse();
    }

    [Fact(DisplayName = "Registered Types Should List Every Registered Map")]
    public void RegisteredTypes_ShouldListEveryRegisteredMap()
    {
        var registry = new QueryMapRegistry();
        registry.Register(Map()).IsSuccess.Should().BeTrue();
        registry.Register(OtherMap()).IsSuccess.Should().BeTrue();

        registry.Count.Should().Be(2);
        registry.RegisteredTypes.Should().BeEquivalentTo([typeof(Target), typeof(OtherTarget)]);
    }

    [Fact(DisplayName = "Registry Should Keep Maps Separate By Type")]
    public void Registry_ShouldKeepMapsSeparateByType()
    {
        var registry = new QueryMapRegistry();
        QueryMap<Target> first = Map();
        QueryMap<OtherTarget> second = OtherMap();

        registry.Register(first).IsSuccess.Should().BeTrue();
        registry.Register(second).IsSuccess.Should().BeTrue();

        registry.Get<Target>().Value.Should().BeSameAs(first);
        registry.Get<OtherTarget>().Value.Should().BeSameAs(second);
    }

    [Fact(DisplayName = "Parse When Map Is Registered Should Use Registered Map")]
    public void Parse_WhenMapIsRegistered_ShouldUseRegisteredMap()
    {
        var registry = new QueryMapRegistry();
        registry.Register(Map()).IsSuccess.Should().BeTrue();

        Result<Query<Target>> result = QueryParser.Parse<Target>(
            new QueryParameters { OrderBy = "score desc" },
            registry);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "Parse When Map Is Not Registered Should Fail Before Parsing")]
    public void Parse_WhenMapIsNotRegistered_ShouldFailBeforeParsing()
    {
        var registry = new QueryMapRegistry();

        Result<Query<Target>> result = QueryParser.Parse<Target>(
            new QueryParameters { Filter = "name==x" },
            registry);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Map.NotRegistered");
    }

    [Fact(DisplayName = "Parse When Provider Is Null Should Throw")]
    public void Parse_WhenProviderIsNull_ShouldThrow()
    {
        Action act = () => QueryParser.Parse<Target>(
            new QueryParameters(),
            provider: null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor With Definitions Should Populate Every Map")]
    public void Constructor_WithDefinitions_ShouldPopulateRegistry()
    {
        // Act
        var registry = new QueryMapRegistry(
        [
            new TargetDefinition(),
            new OtherTargetDefinition()
        ]);

        // Assert
        registry.Count.Should().Be(2);
        registry.IsRegistered<Target>().Should().BeTrue();
        registry.IsRegistered<OtherTarget>().Should().BeTrue();
    }

    [Fact(DisplayName = "Constructor With Duplicate Definitions Should Throw")]
    public void Constructor_WithDuplicateDefinitions_ShouldThrow()
    {
        // Act
        Action act = () => _ = new QueryMapRegistry(
        [
            new TargetDefinition(),
            new TargetDefinition()
        ]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Query.Map.AlreadyRegistered*");
    }

    [Fact(DisplayName = "Constructor With Null Definitions Should Throw")]
    public void Constructor_WithNullDefinitions_ShouldThrow()
    {
        // Act
        Action act = () => _ = new QueryMapRegistry((IEnumerable<IQueryMapDefinition>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TargetDefinition : IQueryMapDefinition
    {
        public Result Register(QueryMapRegistry registry) => registry.Register(Map());
    }

    private sealed class OtherTargetDefinition : IQueryMapDefinition
    {
        public Result Register(QueryMapRegistry registry) => registry.Register(OtherMap());
    }
}
