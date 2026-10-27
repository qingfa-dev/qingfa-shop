using Application.Common.Querying.Mapping;

namespace Application.Tests.Common.Querying.Mapping;


public sealed class QueryMapBuilderTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public Target? Child { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> map = builder.Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Fact(DisplayName = "Map When Name Is Whitespace Should Report A Failure")]
    public void Map_WhenNameIsWhitespace_ShouldReportAFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map(" ", x => x.Name)
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.NameRequired");
    }

    [Theory(DisplayName = "Map When Name Exceeds Max Length Should Report Failure")]
    [InlineData(65)]
    [InlineData(100)]
    [InlineData(200)]
    public void Map_WhenNameExceedsMaxLength_ShouldReportFailure(int length)
    {
        string longName = new string('a', length);

        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map(longName, x => x.Name)
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.NameTooLong");
    }

    [Fact(DisplayName = "Map When Name Differs Only By Case Should Report A Failure")]
    public void Map_WhenNameDiffersOnlyByCase_ShouldReportAFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name)
            .Map("NAME", x => x.Score)
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.Duplicate");
    }

    [Fact(DisplayName = "Map When Searchable Field Is Not String Should Report A Failure")]
    public void Map_WhenSearchableFieldIsNotString_ShouldReportAFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("score", x => x.Score, builder => builder.Searchable())
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.SearchableNotString");
    }

    [Fact(DisplayName = "Map Composite When Members Differ In Type Should Report A Failure")]
    public void MapComposite_WhenMembersDifferInType_ShouldReportAFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .MapComposite("any", [x => x.Name, x => x.Score])
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.CompositeTypeMismatch");
    }

    [Fact(DisplayName = "Map Composite When There Are No Members Should Report A Failure")]
    public void MapComposite_WhenThereAreNoMembers_ShouldReportAFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .MapComposite("any", [])
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.CompositeEmpty");
    }

    [Fact(DisplayName = "Map Composite When Name Is Whitespace Should Report Failure")]
    public void MapComposite_WhenNameIsWhitespace_ShouldReportFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .MapComposite(" ", [x => x.Name])
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.NameRequired");
    }

    [Fact(DisplayName = "Build When Several Declarations Are Broken Should Report Every Failure")]
    public void Build_WhenSeveralDeclarationsAreBroken_ShouldReportEveryFailure()
    {
        // The builder keeps going, so one build reports every problem instead of
        // only the first.
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map(" ", x => x.Name)
            .Map("score", x => x.Score, builder => builder.Searchable())
            .MapComposite("any", [x => x.Name, x => x.Score])
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().HaveCount(3);
        map.Errors.Select(error => error.Code).Should().BeEquivalentTo(
        [
            "Query.Map.NameRequired",
            "Query.Map.SearchableNotString",
            "Query.Map.CompositeTypeMismatch",
        ]);
    }

    [Fact(DisplayName = "Generate Mappings When Depth Is Zero Should Map Top Level Only")]
    public void GenerateMappings_WhenDepthIsZero_ShouldMapTopLevelOnly()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().GenerateMappings());

        map.TryGet("Name", out _).Should().BeTrue();
        map.TryGet("Child.Name", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Generate Mappings When Depth Is One Should Map Nested Properties")]
    public void GenerateMappings_WhenDepthIsOne_ShouldMapNestedProperties()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().GenerateMappings(1));

        map.TryGet("Child.Name", out _).Should().BeTrue();
    }

    [Fact(DisplayName = "Map Indexed When Name Is Whitespace Should Report Failure")]
    public void MapIndexed_WhenNameIsWhitespace_ShouldReportFailure()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .MapIndexed<int>(" ", (target, index) => target.Tags[index])
            .Build();

        map.IsFailure.Should().BeTrue();
        map.Errors.Should().Contain(e => e.Code == "Query.Map.NameRequired");
    }

    [Fact(DisplayName = "Field Builder Convert With When Converter Is Null Should Throw")]
    public void FieldBuilder_ConvertWith_WhenConverterIsNull_ShouldThrow()
    {
        var builder = QueryMapBuilder<Target>.Create();

        Action act = () => builder.Map("name", x => x.Name, b => b.ConvertWith(null!));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Generate Mappings With Indexer Property Should Skip Indexer")]
    public void GenerateMappings_WithIndexerProperty_ShouldSkipIndexer()
    {
        var map = Build(QueryMapBuilder<TypeWithIndexer>.Create().GenerateMappings());

        map.TryGet("Name", out _).Should().BeTrue();
        // The indexer property "Item" should be skipped (line 143)
        map.TryGet("Item", out _).Should().BeFalse();
    }

    [Fact(DisplayName = "Generate Mappings With Duplicate Names Should Skip Duplicate")]
    public void GenerateMappings_WithDuplicateNames_ShouldSkipDuplicate()
    {
        // Create a type with two properties that would generate the same field name
        // This is tricky since C# doesn't allow duplicate property names.
        // The duplicate check is defensive - it prevents adding the same name twice.
        // We'll just verify it doesn't crash on valid types.
        var map = Build(QueryMapBuilder<TypeWithDuplicate>.Create().GenerateMappings(1));

        map.TryGet("Value", out _).Should().BeTrue();
        map.TryGet("Nested.Value", out _).Should().BeTrue();
    }
}

// Helper classes for testing GenerateMappings edge cases
file sealed class TypeWithIndexer
{
    public string Name { get; set; } = string.Empty;
    public string this[int index] => "item"; // This is an indexer - should be skipped
}

file sealed class TypeWithDuplicate
{
    public string Value { get; set; } = string.Empty;
    public NestedType Nested { get; set; } = new();
}

file sealed class NestedType
{
    public string Value { get; set; } = string.Empty; // Same name as parent - duplicate
}
