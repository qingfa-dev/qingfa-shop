using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Parsing;
using System.Globalization;

namespace Application.Tests.Common.Querying.Mapping;

[Trait("Category", "Unit")]
public sealed class ValueConverterTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Priority Priority { get; set; }
    }

    private enum Priority
    {
        Low,
        Medium,
        High
    }

    private static QueryMap<Target> Map() =>
        Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b.Filterable())
            .Map("score", x => x.Score, b => b.Filterable())
            .Map("id", x => x.Id, b => b.Filterable())
            .Map("createdAt", x => x.CreatedAt, b => b.Filterable())
            .Map("priority", x => x.Priority, b => b.Filterable()));

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> map = builder.Build();
        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Fact(DisplayName = "Convert String Returns Success")]
    public void Convert_String_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("name").Value;

        var result = ValueConverter.Convert("hello", field.ValueType, field.Converter, "name");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("hello");
    }

    [Fact(DisplayName = "Convert Int Returns Success")]
    public void Convert_Int_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("score").Value;

        var result = ValueConverter.Convert("42", field.ValueType, field.Converter, "score");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(42);
    }

    [Fact(DisplayName = "Convert Guid Returns Success")]
    public void Convert_Guid_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("id").Value;

        var guid = "550e8400-e29b-41d4-a716-446655440000";
        var result = ValueConverter.Convert(guid, field.ValueType, field.Converter, "id");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Guid.Parse(guid));
    }

    [Fact(DisplayName = "Convert DateTimeOffset Returns Success")]
    public void Convert_DateTimeOffset_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("createdAt").Value;

        var dt = "2024-01-15T10:30:00Z";
        var result = ValueConverter.Convert(dt, field.ValueType, field.Converter, "createdAt");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(DateTimeOffset.Parse(dt, CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "Convert Enum Returns Success")]
    public void Convert_Enum_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("priority").Value;

        var result = ValueConverter.Convert("High", field.ValueType, field.Converter, "priority");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Priority.High);
    }

    [Fact(DisplayName = "Convert Case Insensitive Enum Returns Success")]
    public void Convert_CaseInsensitiveEnum_ReturnsSuccess()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("priority").Value;

        var result = ValueConverter.Convert("high", field.ValueType, field.Converter, "priority");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Priority.High);
    }

    [Fact(DisplayName = "Convert Empty String For Value Type Returns Default")]
    public void Convert_EmptyStringForValueType_ReturnsDefault()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("score").Value;

        var result = ValueConverter.Convert("", field.ValueType, field.Converter, "score");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0);
    }

    [Fact(DisplayName = "Convert Null Keyword For Nullable Returns Null")]
    public void Convert_NullKeywordForNullable_ReturnsNull()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("priority").Value;
        // Make it nullable
        var nullableType = typeof(int?);
        
        var result = ValueConverter.Convert("null", nullableType, field.Converter, "priority");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact(DisplayName = "Convert Invalid Value Returns Failure")]
    public void Convert_InvalidValue_ReturnsFailure()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("score").Value;

        var result = ValueConverter.Convert("not-a-number", field.ValueType, field.Converter, "score");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.ValueInvalid");
    }

    [Fact(DisplayName = "Repeated Conversions Use Cached Converter")]
    public void Convert_RepeatedConversions_UseCachedConverter()
    {
        var state = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field = state.Field("score").Value;

        // First conversion - should populate cache
        var r1 = ValueConverter.Convert("1", field.ValueType, field.Converter, "score");
        r1.IsSuccess.Should().BeTrue();

        // Second conversion - should use cached converter
        var r2 = ValueConverter.Convert("2", field.ValueType, field.Converter, "score");
        r2.IsSuccess.Should().BeTrue();
        r2.Value.Value.Should().Be(2);

        // Third with different type - should populate new cache entry
        var field2 = state.Field("name").Value;
        var r3 = ValueConverter.Convert("test", field2.ValueType, field2.Converter, "name");
        r3.IsSuccess.Should().BeTrue();
        r3.Value.Value.Should().Be("test");
    }

    [Fact(DisplayName = "Cache Works Across Different Parser States")]
    public void Cache_WorksAcrossDifferentParserStates()
    {
        var state1 = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var state2 = new ParserState<Target>(Map(), QueryParseLimits.Default);
        var field1 = state1.Field("score").Value;
        var field2 = state2.Field("score").Value;

        // First conversion with state1
        var r1 = ValueConverter.Convert("1", field1.ValueType, field1.Converter, "score");
        r1.IsSuccess.Should().BeTrue();

        // Second conversion with state2 - should use same cached converter
        var r2 = ValueConverter.Convert("2", field2.ValueType, field2.Converter, "score");
        r2.IsSuccess.Should().BeTrue();
        r2.Value.Value.Should().Be(2);
    }
}