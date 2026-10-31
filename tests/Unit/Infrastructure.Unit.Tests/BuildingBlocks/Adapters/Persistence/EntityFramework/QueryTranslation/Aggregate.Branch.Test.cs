using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Edge cases for aggregate evaluation and grouped projection.</summary>
[Trait("Category", "Unit")]
public sealed class AggregateBranchTest
{
    private static readonly QueryMap<BranchProbe> Map = BranchProbeMap.Create();
    private static readonly IQueryTranslator<BranchProbe> Translator = new QueryTranslationEfCore<BranchProbe>();

    [Fact(DisplayName = "Count should work on a non-numeric string field")]
    public void Count_OnStringField_ShouldReturnCount()
    {
        var rows = new List<BranchProbe>
        {
            new() { Name = "alpha" },
            new() { Name = "beta" },
        };
        var query = new Query<BranchProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "name", "count")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(2);
    }

    [Fact(DisplayName = "DistinctCount should work on a non-numeric string field")]
    public void DistinctCount_OnStringField_ShouldReturnDistinctCount()
    {
        var rows = new List<BranchProbe>
        {
            new() { Name = "alpha" },
            new() { Name = "alpha" },
            new() { Name = "beta" },
        };
        var query = new Query<BranchProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.DistinctCount, "name", "count")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(2);
    }

    [Theory(DisplayName = "String extrema should use ordinal comparison")]
    [InlineData(AggregateFunction.Minimum, "Alpha")]
    [InlineData(AggregateFunction.Maximum, "charlie")]
    public void Extreme_OnStringField_ShouldReturnOrdinalExtreme(
        AggregateFunction function,
        string expected)
    {
        var rows = new List<BranchProbe>
        {
            new() { Name = "bravo" },
            new() { Name = "Alpha" },
            new() { Name = "charlie" },
        };
        var query = new Query<BranchProbe>
        {
            Aggregate = [new AggregateSpec(function, "name", "result")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(expected);
    }

    [Fact(DisplayName = "Aggregate-only numeric conversion failure should be returned")]
    public void AggregateOnly_WhenValueConversionFails_ShouldFail()
    {
        var rows = new List<BranchProbe> { new() { Number = double.NaN } };
        var query = new Query<BranchProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Average, "number", "avg")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.AggregateUnsupported);
    }

    [Fact(DisplayName = "Grouped aggregate conversion failure should be returned")]
    public void GroupedAggregate_WhenValueConversionFails_ShouldFail()
    {
        var rows = new List<BranchProbe>
        {
            new() { Name = "group", Number = double.NaN },
        };
        var query = new Query<BranchProbe>
        {
            GroupBy = [new GroupByField("name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Average, "number", "avg")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.AggregateUnsupported);
    }

    [Fact(DisplayName = "Grouped aggregate resolution failure should be returned")]
    public void GroupedAggregate_WhenFieldIsNotAggregatable_ShouldFail()
    {
        var rows = new List<BranchProbe> { new() { Name = "group" } };
        var query = new Query<BranchProbe>
        {
            GroupBy = [new GroupByField("name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "name", "total")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.AggregateUnsupported);
    }

    [Fact(DisplayName = "Unknown aggregate function should be rejected")]
    public void Aggregate_WithUnknownFunction_ShouldFail()
    {
        var rows = new List<BranchProbe> { new() { Number = 1 } };
        var query = new Query<BranchProbe>
        {
            Aggregate = [new AggregateSpec((AggregateFunction)999, "number", "unknown")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.AggregateUnsupported);
    }

    [Fact(DisplayName = "Grouped projection should honor a key alias")]
    public void GroupedProjection_WithAliasedKey_ShouldUseAlias()
    {
        var rows = new List<BranchProbe> { new() { Name = "group" } };
        var query = new Query<BranchProbe>
        {
            Select = [new SelectField("name", "displayName")],
            GroupBy = [new GroupByField("name")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle().Which.Name.Should().Be("displayName");
    }

    [Fact(DisplayName = "Grouped projection should omit unselected keys and their values")]
    public void GroupedProjection_WithPartialSelect_ShouldOmitUnselectedKeys()
    {
        var rows = new List<BranchProbe> { new() { Name = "group", NullableScore = 7 } };
        var query = new Query<BranchProbe>
        {
            Select = [new SelectField("name", "name")],
            GroupBy = [new GroupByField("name"), new GroupByField("nullableScore")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle().Which.Name.Should().Be("name");
        result.Value.Rows[0].Values.Should().ContainSingle().Which.Should().Be("group");
    }

    [Fact(DisplayName = "Empty group-by with an aggregate should behave as aggregate-only")]
    public void EmptyGroupBy_WithAggregate_ShouldBeAggregateOnly()
    {
        var rows = new List<BranchProbe> { new() { Number = 2 }, new() { Number = 3 } };
        var query = new Query<BranchProbe>
        {
            GroupBy = [],
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "number", "total")]
        };

        Result<TabularResult> result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().ContainSingle();
        result.Value.Rows[0].Values[0].Should().Be(5m);
    }
}
