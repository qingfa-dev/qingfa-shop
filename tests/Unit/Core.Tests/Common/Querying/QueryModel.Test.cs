using Core.Common.Querying;
using Core.Common.Querying.Pagination;

namespace Core.Tests.Common.Querying;

public sealed class QueryModelTest
{
    [Fact]
    public void Query_WhenTabularMembersAreSet_ShouldReportTabular()
    {
        var query = new Query<object>(
            Filter: null,
            OrderBy: [],
            Page: new PageRequest(1, 20),
            Search: null,
            Select: [new Core.Common.Querying.Projection.SelectField("name", "name")],
            GroupBy: null,
            Aggregate: null);

        query.IsTabular.Should().BeTrue();
    }

    [Fact]
    public void Query_WhenOnlyFilterIsSet_ShouldNotBeTabular()
    {
        var query = new Query<object>(
            Filter: null,
            OrderBy: [],
            Page: new PageRequest(1, 20),
            Search: null,
            Select: null,
            GroupBy: null,
            Aggregate: null);

        query.IsTabular.Should().BeFalse();
    }

    [Fact]
    public void QueryPage_ShouldComputeTotals()
    {
        var page = new QueryPage<int>([1, 2, 3], Page: 2, PageSize: 3, TotalCount: 8);

        page.TotalPages.Should().Be(3);
        page.HasPrevious.Should().BeTrue();
        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public void QueryPage_WhenLastPage_ShouldNotHaveNext()
    {
        var page = new QueryPage<int>([], Page: 3, PageSize: 3, TotalCount: 8);

        page.HasNext.Should().BeFalse();
    }
}