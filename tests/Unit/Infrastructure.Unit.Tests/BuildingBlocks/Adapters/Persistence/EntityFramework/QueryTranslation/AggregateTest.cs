using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

[Trait("Category", "Unit")]
public sealed class AggregateTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>();

    [Fact(DisplayName = "BuildTabularClientSide_WithAggregateOnly_ReturnsSingleRow")]
    public void BuildTabularClientSide_WithAggregateOnly_ReturnsSingleRow()
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "score", "total")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(1);
        result.Value.Columns.Should().HaveCount(1);
        result.Value.Rows[0].Values[0].Should().Be(60);
    }

    [Theory(DisplayName = "Aggregate_Count_ReturnsCorrectCount")]
    [InlineData(AggregateFunction.Count, 3)]
    [InlineData(AggregateFunction.DistinctCount, 2)]
    public void Aggregate_Count_ReturnsCorrectCount(AggregateFunction function, int expected)
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 10 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(function, "score", "result")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(expected);
    }

    [Fact(DisplayName = "Aggregate_Sum_ReturnsCorrectSum")]
    public void Aggregate_Sum_ReturnsCorrectSum()
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "score", "total")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(60);
    }

    [Fact(DisplayName = "Aggregate_Average_ReturnsCorrectAverage")]
    public void Aggregate_Average_ReturnsCorrectAverage()
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Average, "score", "avg")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(20m);
    }

    [Fact(DisplayName = "Aggregate_Minimum_ReturnsCorrectMinimum")]
    public void Aggregate_Minimum_ReturnsCorrectMinimum()
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Minimum, "score", "min")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(10);
    }

    [Fact(DisplayName = "Aggregate_Maximum_ReturnsCorrectMaximum")]
    public void Aggregate_Maximum_ReturnsCorrectMaximum()
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Maximum, "score", "max")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(30);
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithGroupBy_ReturnsGroupedRows")]
    public void BuildTabularClientSide_WithGroupBy_ReturnsGroupedRows()
    {
        var rows = new List<QueryProbe>
        {
            new() { Name = "A", Score = 10 },
            new() { Name = "A", Score = 20 },
            new() { Name = "B", Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "score", "total")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(2);
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithMultipleGroups_ReturnsMultipleRows")]
    public void BuildTabularClientSide_WithMultipleGroups_ReturnsMultipleRows()
    {
        var rows = new List<QueryProbe>
        {
            new() { Name = "A", Score = 10 },
            new() { Name = "A", Score = 20 },
            new() { Name = "B", Score = 30 },
            new() { Name = "B", Score = 40 }
        };
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "score", "count")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(2);
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithSelectAndGroupBy_ReturnsSelectedKeys")]
    public void BuildTabularClientSide_WithSelectAndGroupBy_ReturnsSelectedKeys()
    {
        var rows = new List<QueryProbe>
        {
            new() { Name = "A", Score = 10 },
            new() { Name = "B", Score = 20 }
        };
        var query = new Query<QueryProbe>
        {
            Select = [new SelectField("name", "name")],
            GroupBy = [new GroupByField("name")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().ContainSingle(c => c.Name == "name");
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithSelectNotInGroupBy_ReturnsFieldNotGroupedError")]
    public void BuildTabularClientSide_WithSelectNotInGroupBy_ReturnsFieldNotGroupedError()
    {
        var rows = new List<QueryProbe>
        {
            new() { Name = "A", Score = 10 }
        };
        var query = new Query<QueryProbe>
        {
            Select = [new SelectField("score", "score")],
            GroupBy = [new GroupByField("name")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.FieldNotGrouped");
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithNonGroupableKey_ReturnsNotGroupableError")]
    public void BuildTabularClientSide_WithNonGroupableKey_ReturnsNotGroupableError()
    {
        var map = BuildMapWithNonGroupableField();
        var rows = new List<QueryProbe> { new() { Score = 10 } };
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("score")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithUnsupportedAggregate_ReturnsUnsupportedError")]
    public void BuildTabularClientSide_WithUnsupportedAggregate_ReturnsUnsupportedError()
    {
        var rows = new List<QueryProbe> { new() { Name = "test" } };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "name", "total")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Aggregate.Unsupported");
    }

    [Fact(DisplayName = "BuildTabularClientSide_WithSelectAndAggregateNoGroupBy_ReturnsMixedShapeError")]
    public void BuildTabularClientSide_WithSelectAndAggregateNoGroupBy_ReturnsMixedShapeError()
    {
        var rows = new List<QueryProbe> { new() { Name = "A", Score = 10 } };
        var query = new Query<QueryProbe>
        {
            Select = [new SelectField("name", "name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "score", "count")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Tabular.MixedShape");
    }

    [Fact(DisplayName = "Aggregate_Sum_WithDecimalValues_ReturnsCorrectSum")]
    public void Aggregate_Sum_WithDecimalValues_ReturnsCorrectSum()
    {
        var rows = new List<QueryProbe>
        {
            new() { Score = 10 },
            new() { Score = 20 },
            new() { Score = 30 }
        };
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Sum, "score", "total")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(60m);
    }

    [Fact(DisplayName = "Aggregate_Average_WithEmptyRows_ReturnsZero")]
    public void Aggregate_Average_WithEmptyRows_ReturnsZero()
    {
        var rows = new List<QueryProbe>();
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Average, "score", "avg")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(0m);
    }

    [Fact(DisplayName = "Aggregate_Extreme_WithEmptyRows_ReturnsDefault")]
    public void Aggregate_Extreme_WithEmptyRows_ReturnsDefault()
    {
        var rows = new List<QueryProbe>();
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Minimum, "score", "min")]
        };

        var result = Translator.BuildTabularClientSide(rows, query, Map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rows[0].Values[0].Should().Be(0);
    }

    private static QueryMap<QueryProbe> BuildMapWithNonGroupableField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("score", probe => probe.Score, field => field.Groupable(false));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}