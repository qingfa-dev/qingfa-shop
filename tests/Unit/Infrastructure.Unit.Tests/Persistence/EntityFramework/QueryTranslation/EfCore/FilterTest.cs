using AwesomeAssertions;
using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

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

    [Theory(DisplayName = "ApplyFilter_WithValidOperators_Succeeds")]
    [InlineData(FilterOperator.Equal, "test")]
    [InlineData(FilterOperator.NotEqual, "test")]
    [InlineData(FilterOperator.LessThan, "10")]
    [InlineData(FilterOperator.GreaterThan, "10")]
    [InlineData(FilterOperator.GreaterThanOrEqual, "10")]
    [InlineData(FilterOperator.LessThanOrEqual, "10")]
    [InlineData(FilterOperator.Contains, "hello")]
    [InlineData(FilterOperator.NotContains, "hello")]
    [InlineData(FilterOperator.StartsWith, "hello")]
    [InlineData(FilterOperator.NotStartsWith, "hello")]
    [InlineData(FilterOperator.EndsWith, "hello")]
    [InlineData(FilterOperator.NotEndsWith, "hello")]
    public void ApplyFilter_WithValidOperators_Succeeds(FilterOperator op, string value)
    {
        var filter = new FilterCondition("name", op, value, value, typeof(string), false, null, null);
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory(DisplayName = "ApplyFilter_WithInvalidTypeOperatorCombination_ReturnsTypeMismatchError")]
    [InlineData(FilterOperator.Contains, 123)]
    [InlineData(FilterOperator.StartsWith, 123)]
    [InlineData(FilterOperator.EndsWith, 123)]
    public void ApplyFilter_WithInvalidTypeOperatorCombination_ReturnsTypeMismatchError(FilterOperator op, object value)
    {
        var filter = new FilterCondition("score", op, value.ToString(), value, typeof(int), false, null, null);
        var source = new List<QueryProbe>().AsQueryable();

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.TypeMismatch");
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
        var map = BuildMapWithCollectionField();
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("tags", FilterOperator.Contains, "tag1", "tag1", typeof(string), false, null, null);

        var result = Translator.ApplyFilter(source, filter, map);

        result.IsSuccess.Should().BeTrue();
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
                .Composite(probe => probe.Name)
                .Composite(probe => probe.Tags ?? string.Empty));

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
                .ElementType(typeof(string)));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}