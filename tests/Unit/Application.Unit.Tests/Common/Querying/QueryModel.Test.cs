using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;

namespace Application.Unit.Tests.Common.Querying;

public sealed class QueryModelTest
{
    [Fact(DisplayName = "Query When Tabular Members Are Set Should Report Tabular")]
    public void Query_WhenTabularMembersAreSet_ShouldReportTabular()
    {
        var query = new Query<object>
        {
            Filter = null,
            OrderBy = [],
            Page = new PageRequest(1, 20),
            Search = null,
            Select = [new SelectField("name", "name")],
            GroupBy = null,
            Aggregate = null
        };

        query.IsTabular.Should().BeTrue();
    }

    [Fact(DisplayName = "Query When Only Filter Is Set Should Not Be Tabular")]
    public void Query_WhenOnlyFilterIsSet_ShouldNotBeTabular()
    {
        var query = new Query<object>
        {
            Filter = null,
            OrderBy = [],
            Page = new PageRequest(1, 20),
            Search = null,
            Select = null,
            GroupBy = null,
            Aggregate = null
        };

        query.IsTabular.Should().BeFalse();
    }

    [Fact(DisplayName = "Shape When Select Only Should Be Tabular")]
    public void Shape_WhenSelectOnly_ShouldBeTabular()
    {
        var query = new Query<object>
        {
            Select = [new SelectField("name", "name")],
            GroupBy = null,
            Aggregate = null
        };

        query.Shape.Should().Be(QueryShape.Tabular);
        query.IsTabular.Should().BeTrue();
    }

    [Fact(DisplayName = "Shape When Group By Only Should Be Tabular")]
    public void Shape_WhenGroupByOnly_ShouldBeTabular()
    {
        var query = new Query<object>
        {
            Select = null,
            GroupBy = [new GroupByField("kind")],
            Aggregate = null
        };

        query.Shape.Should().Be(QueryShape.Tabular);
        query.IsTabular.Should().BeTrue();
    }

    [Fact(DisplayName = "Shape When Aggregate Only Should Be Scalar Aggregate")]
    public void Shape_WhenAggregateOnly_ShouldBeScalarAggregate()
    {
        var query = new Query<object>
        {
            Select = null,
            GroupBy = null,
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "name", "cnt")]
        };

        query.Shape.Should().Be(QueryShape.ScalarAggregate);
        query.IsTabular.Should().BeTrue();
    }

    [Fact(DisplayName = "Shape When Select And Group By Should Be Tabular")]
    public void Shape_WhenSelectAndGroupBy_ShouldBeTabular()
    {
        var query = new Query<object>
        {
            Select = [new SelectField("kind", "kind")],
            GroupBy = [new GroupByField("kind")],
            Aggregate = null
        };

        query.Shape.Should().Be(QueryShape.Tabular);
        query.IsTabular.Should().BeTrue();
    }

    [Fact(DisplayName = "Shape When Aggregate And Group By Should Be Tabular")]
    public void Shape_WhenAggregateAndGroupBy_ShouldBeTabular()
    {
        var query = new Query<object>
        {
            Select = null,
            GroupBy = [new GroupByField("kind")],
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "name", "cnt")]
        };

        query.Shape.Should().Be(QueryShape.Tabular);
        query.IsTabular.Should().BeTrue();
    }

    [Fact(DisplayName = "Shape When All Null Should Be Entity")]
    public void Shape_WhenAllNull_ShouldBeEntity()
    {
        var query = new Query<object>
        {
            Select = null,
            GroupBy = null,
            Aggregate = null
        };

        query.Shape.Should().Be(QueryShape.Entity);
        query.IsTabular.Should().BeFalse();
    }

    [Fact(DisplayName = "Query Page Should Compute Totals")]
    public void QueryPage_ShouldComputeTotals()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 2, PageSize: 3, TotalCount: 8);

        page.TotalPages.Should().Be(3);
        page.HasPrevious.Should().BeTrue();
        page.HasNext.Should().BeTrue();
    }

    [Fact(DisplayName = "Query Page When Last Page Should Not Have Next")]
    public void QueryPage_WhenLastPage_ShouldNotHaveNext()
    {
        var page = new QueryPage<int>([], Page: 3, PageSize: 3, TotalCount: 8);

        page.HasNext.Should().BeFalse();
    }

    [Fact(DisplayName = "Query Page When First Page Should Not Have Previous")]
    public void QueryPage_WhenFirstPage_ShouldNotHavePrevious()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 3, TotalCount: 8);

        page.HasPrevious.Should().BeFalse();
    }

    [Fact(DisplayName = "Query Page When Single Page Should Have No Previous Or Next")]
    public void QueryPage_WhenSinglePage_ShouldHaveNoPreviousOrNext()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 10, TotalCount: 3);

        page.TotalPages.Should().Be(1);
        page.HasPrevious.Should().BeFalse();
        page.HasNext.Should().BeFalse();
    }

    [Fact(DisplayName = "Query Page When Page Size Is Zero Should Return Zero Total Pages")]
    public void QueryPage_WhenPageSizeIsZero_ShouldReturnZeroTotalPages()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 1, PageSize: 0, TotalCount: 3);

        page.TotalPages.Should().Be(0);
        page.HasNext.Should().BeFalse();
    }

    [Fact(DisplayName = "Query Page When Total Count Is Zero Should Have No Pages")]
    public void QueryPage_WhenTotalCountIsZero_ShouldHaveNoPages()
    {
        var page = new QueryPage<int>([], Page: 1, PageSize: 10, TotalCount: 0);

        page.TotalPages.Should().Be(0);
        page.HasPrevious.Should().BeFalse();
        page.HasNext.Should().BeFalse();
    }

    [Fact(DisplayName = "Query Page When Middle Page Should Have Both Previous And Next")]
    public void QueryPage_WhenMiddlePage_ShouldHaveBothPreviousAndNext()
    {
        var page = new QueryPage<int>([4, 5, 6], Page: 2, PageSize: 3, TotalCount: 9);

        page.TotalPages.Should().Be(3);
        page.HasPrevious.Should().BeTrue();
        page.HasNext.Should().BeTrue();
    }
}