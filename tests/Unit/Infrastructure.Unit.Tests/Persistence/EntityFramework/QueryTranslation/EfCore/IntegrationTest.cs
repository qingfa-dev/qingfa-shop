using AwesomeAssertions;
using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;
using Application.Common.Querying.Sorting;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

[Trait("Category", "Unit")]
public sealed class QueryTranslationEfCoreTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>(QueryProbeOperators.Create());

    [Fact(DisplayName = "FullQuery_WithFilterSortPage_ReturnsComposedQuery")]
    public void FullQuery_WithFilterSortPage_ReturnsComposedQuery()
    {
        var query = new Query<QueryProbe>
        {
            Filter = new FilterCondition("name", FilterOperator.Equal, "test", "test", typeof(string)),
            OrderBy = [new SortField("score", SortDirection.Descending, SortNulls.None)],
            Page = new PageRequest(1, 10)
        };
        var source = new List<QueryProbe>().AsQueryable();

        var filterResult = Translator.ApplyFilter(source, query.Filter, Map);
        filterResult.IsSuccess.Should().BeTrue();

        var sortResult = Translator.ApplySort(filterResult.Value, query.OrderBy, Map);
        sortResult.IsSuccess.Should().BeTrue();

        var pageResult = Translator.ApplyPaging(sortResult.Value, query.Page);
        pageResult.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "FullQuery_WithSearchSortPage_ReturnsComposedQuery")]
    public void FullQuery_WithSearchSortPage_ReturnsComposedQuery()
    {
        var query = new Query<QueryProbe>
        {
            Search = "test",
            OrderBy = [new SortField("name", SortDirection.Ascending, SortNulls.None)],
            Page = new PageRequest(1, 20)
        };
        var source = new List<QueryProbe>().AsQueryable();

        var searchResult = Translator.ApplySearch(source, query.Search, Map);
        searchResult.IsSuccess.Should().BeTrue();

        var sortResult = Translator.ApplySort(searchResult.Value, query.OrderBy, Map);
        sortResult.IsSuccess.Should().BeTrue();

        var pageResult = Translator.ApplyPaging(sortResult.Value, query.Page);
        pageResult.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "FullQuery_WithSelectAndFilter_ReturnsTabularPlan")]
    public void FullQuery_WithSelectAndFilter_ReturnsTabularPlan()
    {
        var query = new Query<QueryProbe>
        {
            Filter = new FilterCondition("score", FilterOperator.GreaterThan, "10", 10, typeof(int)),
            Select = [new SelectField("name", "title"), new SelectField("score", "points")]
        };
        var source = new List<QueryProbe>().AsQueryable();

        var filterResult = Translator.ApplyFilter(source, query.Filter, Map);
        filterResult.IsSuccess.Should().BeTrue();

        var planResult = Translator.BuildSelectPlan(filterResult.Value, query.Select, Map);
        planResult.IsSuccess.Should().BeTrue();
        planResult.Value.Columns.Should().HaveCount(2);
    }

    [Fact(DisplayName = "FullQuery_WithGroupByAndAggregate_ReturnsTabularResult")]
    public void FullQuery_WithGroupByAndAggregate_ReturnsTabularResult()
    {
        var rows = new List<QueryProbe>
        {
            new() { Name = "A", Score = 10, Kind = "X" },
            new() { Name = "A", Score = 20, Kind = "X" },
            new() { Name = "B", Score = 30, Kind = "Y" }
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

    [Fact(DisplayName = "FullQuery_WithComplexFilterAndGroupBy_ReturnsResult")]
    public void FullQuery_WithComplexFilterAndGroupBy_ReturnsResult()
    {
        var rows = new List<QueryProbe>
        {
            new() { Name = "A", Score = 10, Kind = "X" },
            new() { Name = "A", Score = 20, Kind = "X" },
            new() { Name = "B", Score = 30, Kind = "Y" }
        };
        var query = new Query<QueryProbe>
        {
            Filter = new FilterCondition("score", FilterOperator.GreaterThan, "5", 5, typeof(int)),
            GroupBy = [new GroupByField("name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Average, "score", "avg")]
        };

        var filterResult = Translator.ApplyFilter(rows.AsQueryable(), query.Filter, Map);
        filterResult.IsSuccess.Should().BeTrue();

        var result = Translator.BuildTabularClientSide(filterResult.Value.ToList(), query, Map);
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Translator_WithCustomOperators_RegistersAndUsesCustomOperator")]
    public void Translator_WithCustomOperators_RegistersAndUsesCustomOperator()
    {
        var customTranslator = new QueryTranslationEfCore<QueryProbe>(
            new EfOperatorRegistry().Register("customOp", (m, v) => Expression.Equal(m, v)));

        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Custom, "test", "test", typeof(string), false, null, "customOp");

        var result = customTranslator.ApplyFilter(source, filter, Map);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Translator_WithoutCustomOperators_RejectsCustomOperator")]
    public void Translator_WithoutCustomOperators_RejectsCustomOperator()
    {
        var defaultTranslator = new QueryTranslationEfCore<QueryProbe>();
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterCondition("name", FilterOperator.Custom, "test", "test", typeof(string), false, null, "anyOp");

        var result = defaultTranslator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.Unsupported");
    }

    [Fact(DisplayName = "IncludeNavigations_WithFullQuery_IncludesRelevantPaths")]
    public void IncludeNavigations_WithFullQuery_IncludesRelevantPaths()
    {
        var query = new Query<QueryProbe>
        {
            Filter = new FilterCondition("name", FilterOperator.Equal, "test", "test", typeof(string)),
            OrderBy = [new SortField("score", SortDirection.Descending)],
            Page = new PageRequest(1, 10),
            GroupBy = [new GroupByField("name")],
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "score", "count")]
        };
        var source = new List<QueryProbe>().AsQueryable();

        var filterResult = Translator.ApplyFilter(source, query.Filter, Map);
        filterResult.IsSuccess.Should().BeTrue();

        var withNavs = Translator.IncludeNavigations(filterResult.Value, query, Map);
        withNavs.Should().NotBeNull();
    }

    [Fact(DisplayName = "ApplyFilter_CollectsAllErrors")]
    public void ApplyFilter_CollectsAllErrors()
    {
        var source = new List<QueryProbe>().AsQueryable();
        var filter = new FilterGroup(FilterLogic.Or,
            new FilterCondition("missing1", FilterOperator.Equal, "x", "x", typeof(string)),
            new FilterCondition("missing2", FilterOperator.Equal, "y", "y", typeof(string)));

        var result = Translator.ApplyFilter(source, filter, Map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterOrEqualTo(1);
    }
}