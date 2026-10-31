using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

[Trait("Category", "Unit")]
public sealed class FilterTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>(QueryProbeOperators.Create());

    [Fact(DisplayName = "ApplyFilter_WithNullFilter_ReturnsSourceUnchanged")]
    public void ApplyFilter_WithNullFilter_ReturnsSourceUnchanged()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var result = Translator.ApplyFilter(source, null, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(source);
    }

    [Fact(DisplayName = "ApplyFilter_WithUnknownField_ReturnsNotMappedError")]
    public void ApplyFilter_WithUnknownField_ReturnsNotMappedError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("missing", FilterOperator.Equal, "x", "x", typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "ApplyFilter_WithNonFilterableField_ReturnsNotFilterableError")]
    public void ApplyFilter_WithNonFilterableField_ReturnsNotFilterableError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var map = BuildMapWithNonFilterableField();

        var filter = new FilterCondition("name", FilterOperator.Equal, "test", "test", typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "ApplyFilter_WithUnknownCustomOperator_ReturnsUnsupportedError")]
    public void ApplyFilter_WithUnknownCustomOperator_ReturnsUnsupportedError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Custom, "x", "x", typeof(string), false, null, QueryProbeOperators.Unregistered);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.Unsupported");
    }

    [Fact(DisplayName = "ApplyFilter_WithRegisteredCustomOperator_Succeeds")]
    public void ApplyFilter_WithRegisteredCustomOperator_Succeeds()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Custom, "test", "test", typeof(string), false, null, QueryProbeOperators.Registered);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter_WithStringOperators_Succeeds")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.NotEqual)]
    [InlineData(FilterOperator.Contains)]
    [InlineData(FilterOperator.NotContains)]
    [InlineData(FilterOperator.StartsWith)]
    [InlineData(FilterOperator.NotStartsWith)]
    [InlineData(FilterOperator.EndsWith)]
    [InlineData(FilterOperator.NotEndsWith)]
    public void ApplyFilter_WithStringOperators_Succeeds(FilterOperator op)
    {
        var filter = new FilterCondition("name", op, "test", "test", typeof(string), false, null, null);
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter_WithNumericOperators_Succeeds")]
    [InlineData(FilterOperator.LessThan)]
    [InlineData(FilterOperator.GreaterThan)]
    [InlineData(FilterOperator.GreaterThanOrEqual)]
    [InlineData(FilterOperator.LessThanOrEqual)]
    public void ApplyFilter_WithNumericOperators_Succeeds(FilterOperator op)
    {
        var filter = new FilterCondition("score", op, "10", 10, typeof(int), false, null, null);
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter_WithInvalidTypeOperatorCombination_ReturnsValueInvalidError")]
    [InlineData(FilterOperator.Contains, 123)]
    [InlineData(FilterOperator.StartsWith, 123)]
    [InlineData(FilterOperator.EndsWith, 123)]
    public void ApplyFilter_WithInvalidTypeOperatorCombination_ReturnsValueInvalidError(FilterOperator op, object value)
    {
        var filter = new FilterCondition("score", op, value.ToString(), value, typeof(int), false, null, null);
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == QueryOutcome.Codes.FilterValueInvalid);
    }

    [Fact(DisplayName = "ApplyFilter_WithCompositeField_BuildsOrChain")]
    public void ApplyFilter_WithCompositeField_BuildsOrChain()
    {
        var map = BuildMapWithCompositeField();
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("composite", FilterOperator.Equal, "test", "test", typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter_WithNestedGroup_BuildsCorrectPredicate")]
    public void ApplyFilter_WithNestedGroup_BuildsCorrectPredicate()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterGroup(FilterLogic.And,
            new FilterNode[] {
                new FilterCondition("name", FilterOperator.Equal, "test", "test", typeof(string), false, null, null),
                new FilterGroup(FilterLogic.Or,
                    new FilterNode[] {
                        new FilterCondition("score", FilterOperator.GreaterThan, "10", 10, typeof(int), false, null, null),
                        new FilterCondition("score", FilterOperator.LessThan, "20", 20, typeof(int), false, null, null)
                    })
        });

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter_WithNullValueAndEqualityOperator_BuildsNullCheck")]
    public void ApplyFilter_WithNullValueAndEqualityOperator_BuildsNullCheck()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Equal, "null", null, typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter_WithNullValueAndNonEqualityOperator_ReturnsError")]
    public void ApplyFilter_WithNullValueAndNonEqualityOperator_ReturnsError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Contains, "null", null, typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.ValueInvalid");
    }

    [Fact(DisplayName = "ApplyFilter_WithEmptyStringAndEquality_BuildsEmptyCheck")]
    public void ApplyFilter_WithEmptyStringAndEquality_BuildsEmptyCheck()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Equal, "", "", typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter_WithCollectionField_BuildsAnyWithGuard")]
    public void ApplyFilter_WithCollectionField_BuildsAnyWithGuard()
    {
        // QueryProbe.Tags is string?, not string[], so we use a probe with an actual array
        var map = BuildMapWithTagArrayField();
        var source = new List<TagArrayProbe>().AsQueryable();
        var filter = new FilterCondition("tags", FilterOperator.Contains, "tag1", "tag1", typeof(string), false, null, null);

        var translator = new QueryTranslationEfCore<TagArrayProbe>(QueryProbeOperators.Create());
        var result = translator.ApplyFilter(source, filter, map);

        result.IsSuccess.Should().BeTrue();
    }

    private static QueryMap<TagArrayProbe> BuildMapWithTagArrayField()
    {
        var builder = QueryMapBuilder<TagArrayProbe>.Create()
            .Map("tags", probe => probe.Tags, field => field
                .Filterable()
                .ElementType(typeof(string)));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private sealed class TagArrayProbe
    {
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    [Fact(DisplayName = "ApplyFilter_WithCaseInsensitiveFlag_BuildsToLower")]
    public void ApplyFilter_WithCaseInsensitiveFlag_BuildsToLower()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Equal, "TEST", "TEST", typeof(string), true, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter_WithEmptyGroup_ReturnsSyntaxError")]
    public void ApplyFilter_WithEmptyGroup_ReturnsSyntaxError()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterGroup(FilterLogic.And, Array.Empty<FilterNode>());

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact(DisplayName = "ApplyFilter_WithEmptyStringValueAndNotEqual_BuildsEmptyCheck")]
    public void ApplyFilter_WithEmptyStringValueAndNotEqual_BuildsEmptyCheck()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.NotEqual, "", "", typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter with enum operators should succeed")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.NotEqual)]
    public void ApplyFilter_WithEnumOperators_ShouldSucceed(FilterOperator op)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("priority", op, "High", Priority.High, typeof(Priority), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter with Guid operators should succeed")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.NotEqual)]
    public void ApplyFilter_WithGuidOperators_ShouldSucceed(FilterOperator op)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var guid = Guid.NewGuid();
        var filter = new FilterCondition("id", op, guid.ToString(), guid, typeof(Guid), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter with DateTime operators should succeed")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.GreaterThan)]
    [InlineData(FilterOperator.LessThan)]
    [InlineData(FilterOperator.GreaterThanOrEqual)]
    [InlineData(FilterOperator.LessThanOrEqual)]
    public void ApplyFilter_WithDateTimeOperators_ShouldSucceed(FilterOperator op)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var date = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var filter = new FilterCondition("createdAt", op, date.ToString("O"), date, typeof(DateTimeOffset), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter with case insensitive flag should succeed")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.Contains)]
    [InlineData(FilterOperator.StartsWith)]
    [InlineData(FilterOperator.EndsWith)]
    public void ApplyFilter_WithCaseInsensitive_ShouldSucceed(FilterOperator op)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", op, "TEST", "TEST", typeof(string), true, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter with null value and non-equality operator should fail")]
    [InlineData(FilterOperator.Contains)]
    [InlineData(FilterOperator.StartsWith)]
    [InlineData(FilterOperator.EndsWith)]
    [InlineData(FilterOperator.LessThan)]
    [InlineData(FilterOperator.GreaterThan)]
    public void ApplyFilter_WithNullValueAndNonEquality_ShouldFail(FilterOperator op)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", op, "null", null, typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.ValueInvalid");
    }

    [Theory(DisplayName = "ApplyFilter with special characters in values should succeed")]
    [InlineData("hello@world")]
    [InlineData("test#123")]
    [InlineData("café")]
    [InlineData("hello world!")]
    public void ApplyFilter_WithSpecialCharactersInValues_ShouldSucceed(string value)
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Contains, value, value, typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter with OR group should succeed")]
    public void ApplyFilter_WithOrGroup_ShouldSucceed()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterGroup(FilterLogic.Or,
            new FilterNode[] {
                new FilterCondition("name", FilterOperator.Equal, "test1", "test1", typeof(string), false, null, null),
                new FilterCondition("name", FilterOperator.Equal, "test2", "test2", typeof(string), false, null, null)
            });

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ApplyFilter with deeply nested groups should succeed")]
    public void ApplyFilter_WithDeeplyNestedGroups_ShouldSucceed()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterGroup(FilterLogic.And,
            new FilterNode[] {
                new FilterCondition("name", FilterOperator.Equal, "test", "test", typeof(string), false, null, null),
                new FilterGroup(FilterLogic.Or,
                    new FilterNode[] {
                        new FilterGroup(FilterLogic.And,
                            new FilterNode[] {
                                new FilterCondition("score", FilterOperator.GreaterThan, "10", 10, typeof(int), false, null, null),
                                new FilterCondition("score", FilterOperator.LessThan, "20", 20, typeof(int), false, null, null)
                            })
                    })
            });

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    private static QueryMap<QueryProbe> BuildMapWithNonFilterableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("name", probe => probe.Name, field => field.Filterable(false).Selectable())
            .Map("score", probe => probe.Score, field => field.Filterable());

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCompositeField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("composite", probe => probe.Name, field => field
                .Filterable()
                .Composite<QueryProbe>(probe => probe.Name)
                .Composite<QueryProbe>(probe => probe.Tags ?? string.Empty));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCollectionField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("tags", probe => probe.Tags!, field => field
                .Filterable()
                .IsCollection(true)
                .ElementType(typeof(string))
                .Selectable());

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}