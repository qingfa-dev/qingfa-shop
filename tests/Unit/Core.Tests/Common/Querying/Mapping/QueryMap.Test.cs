using System.Reflection;

using AwesomeAssertions;

using Core.Common.Monad.Results;
using Core.Common.Querying.Mapping;

namespace Core.Tests.Common.Querying.Mapping;

public sealed class QueryMapTest
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

    [Fact]
    public void Map_ShouldRecordNamePathAndDefaults()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.Name.Should().Be("name");
        field.ValueType.Should().Be(typeof(string));
        field.Filterable.Should().BeTrue();
        field.Sortable.Should().BeTrue();
        field.Selectable.Should().BeTrue();
        field.Searchable.Should().BeFalse();
        field.Groupable.Should().BeFalse();
    }

    [Fact]
    public void Map_ShouldCaptureNestedMemberPath()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("child.name", x => x.Child!.Name));

        map.TryGet("child.name", out QueryField? field).Should().BeTrue();
        field!.MemberPath.Should().Be("Child.Name");
    }

    [Fact]
    public void Map_WhenCollection_ShouldCaptureElementType()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("tags", x => x.Tags));

        map.TryGet("tags", out QueryField? field).Should().BeTrue();
        field!.ElementType.Should().Be(typeof(string));
        field.IsCollection.Should().BeTrue();
    }

    [Fact]
    public void MapIndexed_ShouldCaptureIndexType()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .MapIndexed<int>("tag", (target, index) => target.Tags[index]));

        map.TryGet("tag", out QueryField? field).Should().BeTrue();
        field!.IndexType.Should().Be(typeof(int));
    }

    [Fact]
    public void MapIndexed_ShouldReportIsIndexed()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .MapIndexed<int>("tag", (target, index) => target.Tags[index]));

        map.TryGet("tag", out QueryField? field).Should().BeTrue();
        field!.IsIndexed.Should().BeTrue();
    }

    [Fact]
    public void Map_WhenNotIndexed_ShouldNotReportIsIndexed()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.IsIndexed.Should().BeFalse();
    }

    [Fact]
    public void Get_WhenTheFieldIsNotMapped_ShouldReturnAFailure()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name));

        Result<QueryField> found = map.Get("name");

        found.IsSuccess.Should().BeTrue();
        found.Value.Name.Should().Be("name");

        Result<QueryField> missing = map.Get("missing");

        missing.IsFailure.Should().BeTrue();
        missing.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void Converter_WhenItFails_ShouldReturnFailure()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("score", x => x.Score, builder => builder.ConvertWith(
                    raw => raw == "high"
                        ? Result<QueryValue>.Success(new QueryValue(10))
                        : Result<QueryValue>.Fail(Error.BadRequest("Probe.Invalid", "bad value")))));

        map.TryGet("score", out QueryField? field).Should().BeTrue();
        field!.Converter!("high").Value.Value.Should().Be(10);
        field.Converter!("low").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ValueConverter_WhenTargetTypeIsUnsupported_ShouldReturnValueInvalidFailure()
    {
        // ValueConverter is internal and the test assembly has no InternalsVisibleTo,
        // so exercise the real conversion path through reflection rather than
        // widening the production API just for this test.
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Core.Common.Querying.Mapping.ValueConverter")!;
        MethodInfo convert = converterType.GetMethod(
            "Convert",
            BindingFlags.Public | BindingFlags.Static)!;

        object? result = convert.Invoke(
            obj: null,
            parameters: ["00:00:00", typeof(TimeSpan), null, "duration"]);

        var failure = (Result<QueryValue>)result!;
        failure.IsFailure.Should().BeTrue();
        failure.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Filter.ValueInvalid");
    }

    [Fact]
    public void FieldBuilder_CaseInsensitive_ShouldBeRecorded()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name, b => b.CaseInsensitive()));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.CaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void FieldBuilder_AllowNullSearch_ShouldBeRecorded()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name, b => b.AllowNullSearch(false)));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.AllowNullSearch.Should().BeFalse();
    }

    [Fact]
    public void MapComposite_WithValidMembers_ShouldDisableSortSelectGroup()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().MapComposite("any", [x => x.Name, x => x.Child!.Name]));

        map.TryGet("any", out QueryField? field).Should().BeTrue();
        field!.Sortable.Should().BeFalse();
        field.Selectable.Should().BeFalse();
        field.Groupable.Should().BeFalse();
    }

    [Fact]
    public void ValueConverter_NullForNonNullableValueType_ShouldFail()
    {
        // ValueConverter is internal, so exercise the real conversion path through reflection.
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Core.Common.Querying.Mapping.ValueConverter")!;
        MethodInfo convert = converterType.GetMethod(
            "Convert",
            BindingFlags.Public | BindingFlags.Static)!;

        // Try to convert "null" to a non-nullable int - should fail
        object? result = convert.Invoke(
            obj: null,
            parameters: ["null", typeof(int), null, "score"]);

        var failure = (Result<QueryValue>)result!;
        failure.IsFailure.Should().BeTrue();
        failure.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Query.Filter.ValueInvalid");
    }
}
