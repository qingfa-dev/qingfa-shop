using Application.Common.Querying;
using Application.Common.Querying.Searching;

namespace Application.Unit.Tests.Common.Querying.Searching;

[Trait("Category", "Unit")]
public sealed class FullTextSearchRequestTest
{
    [Fact(DisplayName = "Constructor With Valid Query Sets Properties")]
    public void Constructor_WithValidQuery_SetsProperties()
    {
        var request = new FullTextSearchRequest("hello world");

        request.Query.Should().Be("hello world");
        request.Mode.Should().Be(SearchMode.FullText);
        request.TermLogic.Should().Be(SearchTermLogic.And); // default from base
        request.CaseInsensitive.Should().BeTrue(); // default from base
        request.Fields.Should().BeNull(); // default from base
    }

    [Theory(DisplayName = "Constructor With Empty Or Whitespace Query Throws")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t \n ")]
    public void Constructor_WithEmptyOrWhitespaceQuery_Throws(string query)
    {
        Action act = () => new FullTextSearchRequest(query);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Search query is required*")
            .And.ParamName.Should().Be("query");
    }

    [Fact(DisplayName = "Constructor With Query Exceeding Max Length Throws")]
    public void Constructor_WithQueryExceedingMaxLength_Throws()
    {
        string longQuery = new('a', QueryConstant.Limits.Search.MaxLength + 1);

        Action act = () => new FullTextSearchRequest(longQuery);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*exceeds {QueryConstant.Limits.Search.MaxLength} characters*")
            .And.ParamName.Should().Be("query");
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var r1 = new FullTextSearchRequest("hello");
        var r2 = new FullTextSearchRequest("hello");

        r1.Should().Be(r2);
    }

    [Fact(DisplayName = "Record Equality Different Query Are Not Equal")]
    public void RecordEquality_DifferentQuery_AreNotEqual()
    {
        var r1 = new FullTextSearchRequest("hello");
        var r2 = new FullTextSearchRequest("world");

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Get Terms Splits Query Into Terms")]
    public void GetTerms_SplitsQueryIntoTerms()
    {
        var request = new FullTextSearchRequest("hello world test");

        var terms = request.GetTerms();

        terms.Should().Equal("hello", "world", "test");
    }

    [Fact(DisplayName = "Base Type Assignability Should Be Assignable To Search Request")]
    public void BaseTypeAssignability_ShouldBeAssignableToSearchRequest()
    {
        SearchRequest request = new FullTextSearchRequest("hello");

        request.Should().BeOfType<FullTextSearchRequest>();
        request.Should().BeAssignableTo<SearchRequest>();
        request.Mode.Should().Be(SearchMode.FullText);
        request.Query.Should().Be("hello");
    }
}