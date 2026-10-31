using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Expressions;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Expressions;

public sealed class QueryExpressionTranslatorProjectionTest
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public bool Active { get; set; }
        public ItemKind Kind { get; set; }
    }

    private enum ItemKind
    {
        First,
        Second,
    }

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("name", item => item.Name, field => field.Searchable())
            .Map("description", item => item.Description, field => field.Searchable())
            .Map("score", item => item.Score)
            .Map("active", item => item.Active, field => field.Groupable())
            .Map("kind", item => item.Kind, field => field.Groupable())
            .Build().Value;

    [Fact(DisplayName = "Order expression should resolve the field")]
    public void TranslateOrder_ShouldResolveField()
    {
        var result = QueryExpressionTranslator<Item>.TranslateOrder(
            item => item.Name, SortDirection.Descending, CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new SortField("name", SortDirection.Descending, SortNulls.None));
    }

    [Fact(DisplayName = "Select expression should use the field name as alias")]
    public void TranslateSelect_ShouldUseFieldAlias()
    {
        var result = QueryExpressionTranslator<Item>.TranslateSelect(
            item => item.Name, CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new SelectField("name", "name"));
    }

    [Fact(DisplayName = "Group expression should expand anonymous members")]
    public void TranslateGroup_WithAnonymous_ShouldReturnKeys()
    {
        var result = QueryExpressionTranslator<Item>.TranslateGroup(
            item => new { item.Active, item.Kind }, CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(key => key.Field)
            .Should().BeEquivalentTo(["active", "kind"]);
    }

    [Fact(DisplayName = "Aggregate expression should use the default alias")]
    public void TranslateAggregate_ShouldUseDefaultAlias()
    {
        var result = QueryExpressionTranslator<Item>.TranslateAggregate(
            AggregateFunction.Count, item => item.Score, null, CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new AggregateSpec(AggregateFunction.Count, "score", "count(score)"));
    }

    [Theory(DisplayName = "Aggregate default alias should use the canonical function token")]
    [InlineData(AggregateFunction.Count, "count(score)")]
    [InlineData(AggregateFunction.Average, "avg(score)")]
    [InlineData(AggregateFunction.Minimum, "min(score)")]
    [InlineData(AggregateFunction.Maximum, "max(score)")]
    [InlineData(AggregateFunction.Sum, "sum(score)")]
    [InlineData(AggregateFunction.DistinctCount, "distinctcount(score)")]
    public void TranslateAggregate_ShouldMatchParserToken(
        AggregateFunction function,
        string expectedAlias)
    {
        var result = QueryExpressionTranslator<Item>.TranslateAggregate(
            function, item => item.Score, null, CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new AggregateSpec(function, "score", expectedAlias));
    }

    [Fact(DisplayName = "Sum should reject a string field")]
    public void TranslateAggregate_SumOnString_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateAggregate(
            AggregateFunction.Sum, item => item.Name, null, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Query.Aggregate.Unsupported");
    }
}