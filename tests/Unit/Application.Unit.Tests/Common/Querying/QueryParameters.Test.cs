using Application.Common.Querying;
using Application.Common.Querying.Searching;

namespace Application.Tests.Common.Querying;

public sealed class QueryParametersTest
{
    [Fact(DisplayName = "QueryParameters: default constructor sets all properties to null")]
    public void Default_AllPropertiesNull()
    {
        var parameters = new QueryParameters();

        parameters.Filter.Should().BeNull();
        parameters.OrderBy.Should().BeNull();
        parameters.Page.Should().BeNull();
        parameters.PageSize.Should().BeNull();
        parameters.Search.Should().BeNull();
        parameters.SearchFields.Should().BeNull();
        parameters.SearchMode.Should().BeNull();
        parameters.SearchTermLogic.Should().BeNull();
        parameters.SearchCaseInsensitive.Should().BeNull();
        parameters.SearchLanguage.Should().BeNull();
        parameters.SearchMinScore.Should().BeNull();
        parameters.Select.Should().BeNull();
        parameters.GroupBy.Should().BeNull();
        parameters.Aggregate.Should().BeNull();
    }

    [Theory(DisplayName = "Filter Accepts Valid String")]
    [InlineData("name=test")]
    [InlineData("name=test,score>10")]
    [InlineData("(name=a|name=b),score>5")]
    public void Filter_AcceptsValidString(string filter)
    {
        var parameters = new QueryParameters { Filter = filter };

        parameters.Filter.Should().Be(filter);
    }

    [Theory(DisplayName = "Order By Accepts Valid String")]
    [InlineData("name desc")]
    [InlineData("name asc,score desc")]
    [InlineData("-name")]
    [InlineData("name? desc")]
    public void OrderBy_AcceptsValidString(string orderBy)
    {
        var parameters = new QueryParameters { OrderBy = orderBy };

        parameters.OrderBy.Should().Be(orderBy);
    }

    [Theory(DisplayName = "Page Accepts Positive Integer")]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Page_AcceptsPositiveInteger(int page)
    {
        var parameters = new QueryParameters { Page = page };

        parameters.Page.Should().Be(page);
    }

    [Theory(DisplayName = "Page Size Accepts Positive Integer")]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void PageSize_AcceptsPositiveInteger(int pageSize)
    {
        var parameters = new QueryParameters { PageSize = pageSize };

        parameters.PageSize.Should().Be(pageSize);
    }

    [Theory(DisplayName = "Search Accepts Valid String")]
    [InlineData("search term")]
    [InlineData("hello world")]
    [InlineData("exact")]
    public void Search_AcceptsValidString(string search)
    {
        var parameters = new QueryParameters { Search = search };

        parameters.Search.Should().Be(search);
    }

    [Theory(DisplayName = "Search Fields Accepts Comma Separated Fields")]
    [InlineData("name")]
    [InlineData("name,description")]
    [InlineData("name, description , other")]
    public void SearchFields_AcceptsCommaSeparatedFields(string fields)
    {
        var parameters = new QueryParameters { SearchFields = fields };

        parameters.SearchFields.Should().Be(fields);
    }

    [Theory(DisplayName = "Search Mode Accepts All Enum Values")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.FullText)]
    [InlineData(SearchMode.Fuzzy)]
    public void SearchMode_AcceptsAllEnumValues(SearchMode mode)
    {
        var parameters = new QueryParameters { SearchMode = mode };

        parameters.SearchMode.Should().Be(mode);
    }

    [Theory(DisplayName = "Search Term Logic Accepts All Enum Values")]
    [InlineData(SearchTermLogic.And)]
    [InlineData(SearchTermLogic.Or)]
    public void SearchTermLogic_AcceptsAllEnumValues(SearchTermLogic logic)
    {
        var parameters = new QueryParameters { SearchTermLogic = logic };

        parameters.SearchTermLogic.Should().Be(logic);
    }

    [Theory(DisplayName = "Search Case Insensitive Accepts Boolean")]
    [InlineData(true)]
    [InlineData(false)]
    public void SearchCaseInsensitive_AcceptsBoolean(bool value)
    {
        var parameters = new QueryParameters { SearchCaseInsensitive = value };

        parameters.SearchCaseInsensitive.Should().Be(value);
    }

    [Theory(DisplayName = "Search Language Accepts Valid String")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("zh-CN")]
    public void SearchLanguage_AcceptsValidString(string language)
    {
        var parameters = new QueryParameters { SearchLanguage = language };

        parameters.SearchLanguage.Should().Be(language);
    }

    [Theory(DisplayName = "Search Min Score Accepts Valid Range")]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void SearchMinScore_AcceptsValidRange(double score)
    {
        var parameters = new QueryParameters { SearchMinScore = score };

        parameters.SearchMinScore.Should().Be(score);
    }

    [Theory(DisplayName = "Select Accepts Valid Projection")]
    [InlineData("name")]
    [InlineData("name as title")]
    [InlineData("name,score as points")]
    public void Select_AcceptsValidProjection(string select)
    {
        var parameters = new QueryParameters { Select = select };

        parameters.Select.Should().Be(select);
    }

    [Theory(DisplayName = "Group By Accepts Valid Fields")]
    [InlineData("name")]
    [InlineData("name,category")]
    public void GroupBy_AcceptsValidFields(string groupBy)
    {
        var parameters = new QueryParameters { GroupBy = groupBy };

        parameters.GroupBy.Should().Be(groupBy);
    }

    [Theory(DisplayName = "Aggregate Accepts Valid Expressions")]
    [InlineData("sum(score)")]
    [InlineData("count(name)")]
    [InlineData("avg(score),max(score)")]
    public void Aggregate_AcceptsValidExpressions(string aggregate)
    {
        var parameters = new QueryParameters { Aggregate = aggregate };

        parameters.Aggregate.Should().Be(aggregate);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var p1 = new QueryParameters
        {
            Filter = "name=test",
            OrderBy = "name desc",
            Page = 2,
            PageSize = 50,
            Search = "hello",
            SearchFields = "name",
            SearchMode = SearchMode.Contains,
            SearchTermLogic = SearchTermLogic.And,
            SearchCaseInsensitive = true,
            SearchLanguage = "en",
            SearchMinScore = 0.5,
            Select = "name",
            GroupBy = "category",
            Aggregate = "count(name)"
        };
        var p2 = new QueryParameters
        {
            Filter = "name=test",
            OrderBy = "name desc",
            Page = 2,
            PageSize = 50,
            Search = "hello",
            SearchFields = "name",
            SearchMode = SearchMode.Contains,
            SearchTermLogic = SearchTermLogic.And,
            SearchCaseInsensitive = true,
            SearchLanguage = "en",
            SearchMinScore = 0.5,
            Select = "name",
            GroupBy = "category",
            Aggregate = "count(name)"
        };

        p1.Should().Be(p2);
    }

    [Fact(DisplayName = "Record Equality Different Values Are Not Equal")]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var p1 = new QueryParameters { Filter = "name=a" };
        var p2 = new QueryParameters { Filter = "name=b" };

        p1.Should().NotBe(p2);
    }
}