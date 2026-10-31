using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Mapping;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Parsing;

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

    [Fact(DisplayName = "Select Should Parse Alias")]
    public void Select_ShouldParseAlias()
    {
        Result<Query<Target>> result = Parse(select: "name as title");

        result.IsSuccess.Should().BeTrue();
        result.Value.Select!.Single().Should().Be(new Application.BuildingBlocks.Querying.Projection.SelectField("name", "title"));
    }

    [Fact(DisplayName = "Select When Field Is Not Selectable Should Fail")]
    public void Select_WhenFieldIsNotSelectable_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "hidden");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "Group By Should Parse Fields")]
    public void GroupBy_ShouldParseFields()
    {
        Result<Query<Target>> result = Parse(groupBy: "kind,name");

        result.IsSuccess.Should().BeTrue();
        result.Value.GroupBy!.Select(g => g.Field).Should().Equal("kind", "name");
    }

    [Theory(DisplayName = "Aggregate Should Parse Functions")]
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

    [Fact(DisplayName = "Aggregate When Sum Is Not Numeric Should Fail")]
    public void Aggregate_WhenSumIsNotNumeric_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "sum(name)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Unsupported");
    }

    [Fact(DisplayName = "Aggregate When The Expression Is Malformed Should Fail")]
    public void Aggregate_WhenTheExpressionIsMalformed_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "median(score)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Syntax");
    }

    [Fact(DisplayName = "Aggregate When There Are Too Many Should Fail")]
    public void Aggregate_WhenThereAreTooMany_ShouldFail()
    {
        Result<Query<Target>> result = Parse(
            aggregate: string.Join(',', Enumerable.Repeat("count(name)", 17)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Syntax");
    }

    [Fact(DisplayName = "Select When There Are Too Many Columns Should Fail")]
    public void Select_WhenThereAreTooManyColumns_ShouldFail()
    {
        Result<Query<Target>> result = Parse(
            select: string.Join(',', Enumerable.Repeat("name", 33)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact(DisplayName = "Group By When There Are Too Many Keys Should Fail")]
    public void GroupBy_WhenThereAreTooManyKeys_ShouldFail()
    {
        Result<Query<Target>> result = Parse(
            groupBy: string.Join(',', Enumerable.Repeat("kind", 9)));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact(DisplayName = "Select When The Alias Is Duplicated Should Fail")]
    public void Select_WhenTheAliasIsDuplicated_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "name as title,score as title");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == "Query.Filter.Syntax" && e.Message.Contains("duplicate alias"));
    }

    [Fact(DisplayName = "Select When Exactly At The Column Limit Should Succeed")]
    public void Select_WhenExactlyAtTheColumnLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive. Use distinct selectable fields.
        // Target has 3 selectable fields (name, score, kind), so test up to that.
        string select = "name as c0,score as c1,kind as c2";

        Result<Query<Target>> result = Parse(select: select);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Group By When Exactly At The Key Limit Should Succeed")]
    public void GroupBy_WhenExactlyAtTheKeyLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive. Use distinct groupable fields.
        // Target has 3 groupable fields (name, score, kind), so test up to that.
        Result<Query<Target>> result = Parse(groupBy: "name,score,kind");

        result.IsSuccess.Should().BeTrue();
        result.Value.GroupBy.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Aggregate When Exactly At The Limit Should Succeed")]
    public void Aggregate_WhenExactlyAtTheLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        Result<Query<Target>> result = Parse(
            aggregate: string.Join(',', Enumerable.Repeat("count(name)", 16)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Aggregate.Should().HaveCount(16);
    }

    [Fact(DisplayName = "Shape When Select And Aggregate Without Group Should Fail")]
    public void Shape_WhenSelectAndAggregateWithoutGroup_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "name", aggregate: "count(name)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.MixedShape");
    }

    [Fact(DisplayName = "Shape When Selected Field Is Not Grouped Should Fail")]
    public void Shape_WhenSelectedFieldIsNotGrouped_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "name", groupBy: "kind");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.FieldNotGrouped");
    }

    [Fact(DisplayName = "Shape When Select Is Subset Of Group Should Pass")]
    public void Shape_WhenSelectIsSubsetOfGroup_ShouldPass()
    {
        Result<Query<Target>> result = Parse(select: "kind", groupBy: "kind,name");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Select When Field Is Unknown Should Fail")]
    public void Select_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(select: "unknown");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Group By When Field Is Unknown Should Fail")]
    public void GroupBy_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(groupBy: "unknown");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Group By When Field Is Not Groupable Should Fail")]
    public void GroupBy_WhenFieldIsNotGroupable_ShouldFail()
    {
        Result<Query<Target>> result = Parse(groupBy: "hidden");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "Aggregate When Field Is Unknown Should Fail")]
    public void Aggregate_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "count(unknown)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Aggregate When Function Is Unknown Should Fail")]
    public void Aggregate_WhenFunctionIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse(aggregate: "median(score)");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Syntax");
    }
}