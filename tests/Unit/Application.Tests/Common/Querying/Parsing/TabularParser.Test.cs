using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Mapping;

using Core.Common.Monad.Results;

namespace Application.Tests.Common.Querying.Parsing;

public sealed class TabularParserTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Hidden { get; set; } = string.Empty;
    }

    private static QueryMap<Target> Map()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b.Groupable().Selectable())
            .Map("score", x => x.Score, b => b.Groupable().Selectable())
            .Map("kind", x => x.Kind, b => b.Groupable().Selectable())
            .Map("hidden", x => x.Hidden, b => b.Selectable(false).Groupable(false))
            .Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    private static Result<Query<Target>> Parse(
        string? select = null,
        string? groupBy = null,
        string? aggregate = null) =>
        QueryParser.Parse(new QueryParameters
        {
            Select = select,
            GroupBy = groupBy,
            Aggregate = aggregate,
        }, Map());

    [Fact]
    public void Select_ShouldParseAlias()
    {
        Result<Query<Target>> result = Parse(select: "name as title");

        result.IsSuccess.Should().BeTrue();
        result.Value.Select!.Single().Should().Be(new Application.Common.Querying.Projection.SelectField("name", "title"));
    }

    [Fact]
    public void Select_WhenFieldIsNotSelectable_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "hidden");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact]
    public void GroupBy_ShouldParseFields()
    {
        Result<Query<Target>> result = Parse(groupBy: "kind,name");

        result.IsSuccess.Should().BeTrue();
        result.Value.GroupBy!.Select(g => g.Field).Should().Equal("kind", "name");
    }

    [Theory]
    [InlineData("sum(score)", AggregateFunction.Sum)]
    [InlineData("avg(score)", AggregateFunction.Average)]
    [InlineData("min(score)", AggregateFunction.Minimum)]
    [InlineData("max(score)", AggregateFunction.Maximum)]
    [InlineData("count(name)", AggregateFunction.Count)]
    [InlineData("distinctCount(name)", AggregateFunction.DistinctCount)]
    public void Aggregate_ShouldParseFunctions(string text, AggregateFunction expected)
    {
        Result<Query<Target>> result = Parse(aggregate: text);

        result.IsSuccess.Should().BeTrue();
        result.Value.Aggregate!.Single().Function.Should().Be(expected);
    }

    [Fact]
    public void Aggregate_WhenSumIsNotNumeric_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "sum(name)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Unsupported");
    }

    [Fact]
    public void Aggregate_WhenTheExpressionIsMalformed_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "median(score)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Syntax");
    }

    [Fact]
    public void Aggregate_WhenThereAreTooMany_ShouldFail()
    {
        Result<Query<Target>> result = Parse(
            aggregate: string.Join(',', Enumerable.Repeat("count(name)", 17)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Syntax");
    }

    [Fact]
    public void Select_WhenThereAreTooManyColumns_ShouldFail()
    {
        Result<Query<Target>> result = Parse(
            select: string.Join(',', Enumerable.Repeat("name", 33)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact]
    public void GroupBy_WhenThereAreTooManyKeys_ShouldFail()
    {
        Result<Query<Target>> result = Parse(
            groupBy: string.Join(',', Enumerable.Repeat("kind", 9)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact]
    public void Select_WhenTheAliasIsDuplicated_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "name as title,score as title");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == "Query.Filter.Syntax" && e.Message.Contains("duplicate alias"));
    }

    [Fact]
    public void Select_WhenExactlyAtTheColumnLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive. Each column needs its own alias,
        // because a repeated field would trip the duplicate-alias rule first.
        string select = string.Join(
            ',', Enumerable.Range(0, 32).Select(index => $"name as c{index}"));

        Result<Query<Target>> result = Parse(select: select);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select.Should().HaveCount(32);
    }

    [Fact]
    public void GroupBy_WhenExactlyAtTheKeyLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        Result<Query<Target>> result = Parse(
            groupBy: string.Join(',', Enumerable.Repeat("kind", 8)));

        result.IsSuccess.Should().BeTrue();
        result.Value.GroupBy.Should().HaveCount(8);
    }

    [Fact]
    public void Aggregate_WhenExactlyAtTheLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        Result<Query<Target>> result = Parse(
            aggregate: string.Join(',', Enumerable.Repeat("count(name)", 16)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Aggregate.Should().HaveCount(16);
    }

    [Fact]
    public void Shape_WhenSelectAndAggregateWithoutGroup_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "name", aggregate: "count(name)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.MixedShape");
    }

    [Fact]
    public void Shape_WhenSelectedFieldIsNotGrouped_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "name", groupBy: "kind");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.FieldNotGrouped");
    }

    [Fact]
    public void Shape_WhenSelectIsSubsetOfGroup_ShouldPass()
    {
        Result<Query<Target>> result = Parse(select: "kind", groupBy: "kind,name");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Select_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "unknown");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void GroupBy_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(groupBy: "unknown");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void GroupBy_WhenFieldIsNotGroupable_ShouldFail()
    {
        Result<Query<Target>> result = Parse(groupBy: "hidden");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact]
    public void Aggregate_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "count(unknown)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void Aggregate_WhenFunctionIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "median(score)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Syntax");
    }
}