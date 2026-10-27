using System.Globalization;
using System.Reflection;


using Application.Common.Querying.Mapping;

namespace Application.Unit.Tests.Common.Querying.Mapping;

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

    [Fact(DisplayName = "Map Should Record Name Path And Defaults")]
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

    [Fact(DisplayName = "Map Should Capture Nested Member Path")]
    public void Map_ShouldCaptureNestedMemberPath()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("child.name", x => x.Child!.Name));

        map.TryGet("child.name", out QueryField? field).Should().BeTrue();
        field!.MemberPath.Should().Be("Child.Name");
    }

    [Fact(DisplayName = "Map When Collection Should Capture Element Type")]
    public void Map_WhenCollection_ShouldCaptureElementType()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("tags", x => x.Tags));

        map.TryGet("tags", out QueryField? field).Should().BeTrue();
        field!.ElementType.Should().Be(typeof(string));
        field.IsCollection.Should().BeTrue();
    }

    [Fact(DisplayName = "Map Indexed Should Capture Index Type")]
    public void MapIndexed_ShouldCaptureIndexType()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .MapIndexed<int>("tag", (target, index) => target.Tags[index]));

        map.TryGet("tag", out QueryField? field).Should().BeTrue();
        field!.IndexType.Should().Be(typeof(int));
    }

    [Fact(DisplayName = "Map Indexed Should Report Is Indexed")]
    public void MapIndexed_ShouldReportIsIndexed()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .MapIndexed<int>("tag", (target, index) => target.Tags[index]));

        map.TryGet("tag", out QueryField? field).Should().BeTrue();
        field!.IsIndexed.Should().BeTrue();
    }

    [Fact(DisplayName = "Map When Not Indexed Should Not Report Is Indexed")]
    public void Map_WhenNotIndexed_ShouldNotReportIsIndexed()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.IsIndexed.Should().BeFalse();
    }

    [Fact(DisplayName = "Get When The Field Is Not Mapped Should Return A Failure")]
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

    [Fact(DisplayName = "Converter When It Fails Should Return Failure")]
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

    [Fact(DisplayName = "Value Converter When Target Type Is Unsupported Should Return Value Invalid Failure")]
    public void ValueConverter_WhenTargetTypeIsUnsupported_ShouldReturnValueInvalidFailure()
    {
        // ValueConverter is internal and the test assembly has no InternalsVisibleTo,
        // so exercise the real conversion path through reflection rather than
        // widening the production API just for this test.
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Application.Common.Querying.Mapping.ValueConverter")!;
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

    [Fact(DisplayName = "Field Builder Case Insensitive Should Be Recorded")]
    public void FieldBuilder_CaseInsensitive_ShouldBeRecorded()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name, b => b.CaseInsensitive()));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.CaseInsensitive.Should().BeTrue();
    }

    [Fact(DisplayName = "Field Builder Allow Null Search Should Be Recorded")]
    public void FieldBuilder_AllowNullSearch_ShouldBeRecorded()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("name", x => x.Name, b => b.AllowNullSearch(false)));

        map.TryGet("name", out QueryField? field).Should().BeTrue();
        field!.AllowNullSearch.Should().BeFalse();
    }

    [Fact(DisplayName = "Map Composite With Valid Members Should Disable Sort Select Group")]
    public void MapComposite_WithValidMembers_ShouldDisableSortSelectGroup()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().MapComposite("any", [x => x.Name, x => x.Child!.Name]));

        map.TryGet("any", out QueryField? field).Should().BeTrue();
        field!.Sortable.Should().BeFalse();
        field.Selectable.Should().BeFalse();
        field.Groupable.Should().BeFalse();
    }

    [Fact(DisplayName = "Value Converter Null For Non Nullable Value Type Should Fail")]
    public void ValueConverter_NullForNonNullableValueType_ShouldFail()
    {
        // ValueConverter is internal, so exercise the real conversion path through reflection.
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Application.Common.Querying.Mapping.ValueConverter")!;
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

    public static TheoryData<string, Type, object> ValueConverterTestData =>
        new()
        {
            { "hello", typeof(string), "hello" },
            { "123", typeof(int), 123 },
            { "123", typeof(long), 123L },
            { "123.45", typeof(double), 123.45 },
            { "123.45", typeof(decimal), 123.45m },
            { "true", typeof(bool), true },
            { "false", typeof(bool), false },
            { "2024-01-15", typeof(DateTime), DateTime.Parse("2024-01-15", CultureInfo.InvariantCulture) },
            { "2024-01-15T10:30:00Z", typeof(DateTimeOffset), DateTimeOffset.Parse("2024-01-15T10:30:00Z", CultureInfo.InvariantCulture) },
            { "2024-01-15", typeof(DateOnly), DateOnly.Parse("2024-01-15", CultureInfo.InvariantCulture) },
            { "10:30:00", typeof(TimeOnly), TimeOnly.Parse("10:30:00", CultureInfo.InvariantCulture) },
            { "550e8400-e29b-41d4-a716-446655440000", typeof(Guid), Guid.Parse("550e8400-e29b-41d4-a716-446655440000") },
        };

    [Theory(DisplayName = "Value Converter Supported Types Should Convert")]
    [MemberData(nameof(ValueConverterTestData))]
    public void ValueConverter_SupportedTypes_ShouldConvert(string input, Type type, object expected)
    {
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Application.Common.Querying.Mapping.ValueConverter")!;
        MethodInfo convert = converterType.GetMethod(
            "Convert",
            BindingFlags.Public | BindingFlags.Static)!;

        object? result = convert.Invoke(
            obj: null,
            parameters: [input, type, null, "field"]);

        var success = (Result<QueryValue>)result!;
        success.IsSuccess.Should().BeTrue();
        success.Value.Value.Should().Be(expected);
    }

    [Fact(DisplayName = "Value Converter Enum Should Convert By Name")]
    public void ValueConverter_Enum_ShouldConvertByName()
    {
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Application.Common.Querying.Mapping.ValueConverter")!;
        MethodInfo convert = converterType.GetMethod(
            "Convert",
            BindingFlags.Public | BindingFlags.Static)!;

        object? result = convert.Invoke(
            obj: null,
            parameters: ["High", typeof(Kind), null, "kind"]);

        var success = (Result<QueryValue>)result!;
        success.IsSuccess.Should().BeTrue();
        success.Value.Value.Should().Be(Kind.High);
    }

    [Fact(DisplayName = "Value Converter Empty String For Nullable Type Returns Null")]
    public void ValueConverter_EmptyString_ForNullableType_ReturnsNull()
    {
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Application.Common.Querying.Mapping.ValueConverter")!;
        MethodInfo convert = converterType.GetMethod(
            "Convert",
            BindingFlags.Public | BindingFlags.Static)!;

        object? result = convert.Invoke(
            obj: null,
            parameters: ["", typeof(string), null, "field"]);

        var success = (Result<QueryValue>)result!;
        success.IsSuccess.Should().BeTrue();
        success.Value.Value.Should().BeNull();
    }

    [Fact(DisplayName = "Value Converter Empty String For Non Nullable Value Type Returns Default")]
    public void ValueConverter_EmptyString_ForNonNullableValueType_ReturnsDefault()
    {
        Type converterType = typeof(QueryValue).Assembly.GetType(
            "Application.Common.Querying.Mapping.ValueConverter")!;
        MethodInfo convert = converterType.GetMethod(
            "Convert",
            BindingFlags.Public | BindingFlags.Static)!;

        object? result = convert.Invoke(
            obj: null,
            parameters: ["", typeof(int), null, "field"]);

        var success = (Result<QueryValue>)result!;
        success.IsSuccess.Should().BeTrue();
        success.Value.Value.Should().Be(0);
    }

    private enum Kind { Low, High }
}
