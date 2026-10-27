using Application.Common.Querying;
using Application.Common.Querying.Searching;

namespace Application.Unit.Tests.Common.Querying.Searching;


public sealed class SearchRequestTest
{
    // SearchRequest is abstract, so we test through BasicSearchRequest
    // but verify the base class behavior

    [Fact(DisplayName = "Constructor With Valid Query And Mode Sets Properties")]
    public void Constructor_WithValidQueryAndMode_SetsProperties()
    {
        var request = new BasicSearchRequest("hello world", SearchMode.Contains);

        request.Query.Should().Be("hello world");
        request.Mode.Should().Be(SearchMode.Contains);
        request.TermLogic.Should().Be(SearchTermLogic.And); // default
        request.CaseInsensitive.Should().BeTrue(); // default
        request.Fields.Should().BeNull(); // default
    }

    [Theory(DisplayName = "Constructor With Empty Or Whitespace Query Throws")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t \n ")]
    public void Constructor_WithEmptyOrWhitespaceQuery_Throws(string query)
    {
        Action act = () => new BasicSearchRequest(query, SearchMode.Contains);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Search query is required*")
            .And.ParamName.Should().Be("query");
    }

    [Fact(DisplayName = "Constructor With Query Exceeding Max Length Throws")]
    public void Constructor_WithQueryExceedingMaxLength_Throws()
    {
        string longQuery = new('a', QueryConstant.Limits.Search.MaxLength + 1);

        Action act = () => new BasicSearchRequest(longQuery, SearchMode.Contains);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*exceeds {QueryConstant.Limits.Search.MaxLength} characters*")
            .And.ParamName.Should().Be("query");
    }

    [Fact(DisplayName = "Constructor With Query At Max Length Succeeds")]
    public void Constructor_WithQueryAtMaxLength_Succeeds()
    {
        string maxQuery = new('a', QueryConstant.Limits.Search.MaxLength);

        var request = new BasicSearchRequest(maxQuery, SearchMode.Contains);

        request.Query.Should().Be(maxQuery);
        request.Query.Length.Should().Be(QueryConstant.Limits.Search.MaxLength);
    }

    [Theory(DisplayName = "Constructor With Non Full Text Mode Accepts All")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.Fuzzy)]
    public void Constructor_WithNonFullTextMode_AcceptsAll(SearchMode mode)
    {
        var request = new BasicSearchRequest("test", mode);

        request.Mode.Should().Be(mode);
    }

    [Fact(DisplayName = "Constructor With Full Text Mode Throws")]
    public void Constructor_WithFullTextMode_Throws()
    {
        Action act = () => new BasicSearchRequest("test", SearchMode.FullText);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Use FullTextSearchRequest for FullText mode*")
            .And.ParamName.Should().Be("mode");
    }

    [Theory(DisplayName = "Get Terms With Multiple Terms Returns All Terms")]
    [InlineData("hello", new[] { "hello" })]
    [InlineData("hello world", new[] { "hello", "world" })]
    [InlineData("hello world test", new[] { "hello", "world", "test" })]
    [InlineData("a b c d e f g h", new[] { "a", "b", "c", "d", "e", "f", "g", "h" })]
    public void GetTerms_WithMultipleTerms_ReturnsAllTerms(string query, string[] expected)
    {
        var request = new BasicSearchRequest(query, SearchMode.Contains);

        var terms = request.GetTerms();

        terms.Should().Equal(expected);
        terms.Should().HaveCount(expected.Length);
    }

    [Theory(DisplayName = "Get Terms With Whitespace Variations Trims Terms")]
    [InlineData("  hello  ", new[] { "hello" })]
    [InlineData("hello   world", new[] { "hello", "world" })]
    [InlineData("  hello   world  test  ", new[] { "hello", "world", "test" })]
    [InlineData("\thello\tworld\n", new[] { "hello", "world" })]
    public void GetTerms_WithWhitespaceVariations_TrimsTerms(string query, string[] expected)
    {
        var request = new BasicSearchRequest(query, SearchMode.Contains);

        var terms = request.GetTerms();

        terms.Should().Equal(expected);
    }

    [Theory(DisplayName = "Properties With All Set Should Persist Values")]
    [InlineData(SearchMode.Contains, "name", SearchTermLogic.And, true)]
    [InlineData(SearchMode.StartsWith, "name,description", SearchTermLogic.Or, false)]
    [InlineData(SearchMode.Exact, "name", SearchTermLogic.And, false)]
    [InlineData(SearchMode.Fuzzy, "description", SearchTermLogic.Or, true)]
    public void Properties_WithAllSet_ShouldPersistValues(
        SearchMode mode, string fieldsCsv, SearchTermLogic termLogic, bool caseInsensitive)
    {
        var fields = fieldsCsv.Split(',', StringSplitOptions.TrimEntries);

        var request = new BasicSearchRequest("test query", mode)
        {
            Fields = fields,
            TermLogic = termLogic,
            CaseInsensitive = caseInsensitive
        };

        request.Query.Should().Be("test query");
        request.Mode.Should().Be(mode);
        request.Fields.Should().Equal(fields);
        request.TermLogic.Should().Be(termLogic);
        request.CaseInsensitive.Should().Be(caseInsensitive);
    }

    [Theory(DisplayName = "Fields With Csv String Should Trim And Set Fields")]
    [InlineData("name")]
    [InlineData("name,description")]
    [InlineData("name, description , other")]
    [InlineData("  name  ,  description  ")]
    public void Fields_WithCsvString_ShouldTrimAndSetFields(string fieldsCsv)
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains)
        {
            Fields = fieldsCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        };

        request.Fields.Should().NotBeNull();
        request.Fields.Should().OnlyContain(f => !string.IsNullOrWhiteSpace(f));
        request.Fields.Should().AllSatisfy(f => f.Should().NotContain(" "));
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var fields = new[] { "name" };
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = fields, TermLogic = SearchTermLogic.And, CaseInsensitive = true };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = fields, TermLogic = SearchTermLogic.And, CaseInsensitive = true };

        r1.Should().Be(r2);
    }

    [Theory(DisplayName = "Record Equality Different Values Are Not Equal")]
    [InlineData("hello", SearchMode.Contains, "world", SearchMode.Contains)]
    [InlineData("hello", SearchMode.Contains, "hello", SearchMode.StartsWith)]
    [InlineData("hello", SearchMode.Contains, "hello", SearchMode.Exact)]
    [InlineData("hello", SearchMode.Contains, "hello", SearchMode.Fuzzy)]
    public void RecordEquality_DifferentValues_AreNotEqual(
        string query1, SearchMode mode1, string query2, SearchMode mode2)
    {
        var r1 = new BasicSearchRequest(query1, mode1) { Fields = new[] { "name" }, TermLogic = SearchTermLogic.And, CaseInsensitive = true };
        var r2 = new BasicSearchRequest(query2, mode2) { Fields = new[] { "name" }, TermLogic = SearchTermLogic.And, CaseInsensitive = true };

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality With Different Fields Are Not Equal")]
    public void RecordEquality_WithDifferentFields_AreNotEqual()
    {
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = new[] { "name" } };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = new[] { "description" } };

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality With Different Term Logic Are Not Equal")]
    public void RecordEquality_WithDifferentTermLogic_AreNotEqual()
    {
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { TermLogic = SearchTermLogic.And };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { TermLogic = SearchTermLogic.Or };

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality With Different Case Insensitive Are Not Equal")]
    public void RecordEquality_WithDifferentCaseInsensitive_AreNotEqual()
    {
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { CaseInsensitive = true };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { CaseInsensitive = false };

        r1.Should().NotBe(r2);
    }

    [Theory(DisplayName = "Base Type Assignability Should Be Assignable To Search Request")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.Fuzzy)]
    public void BaseTypeAssignability_ShouldBeAssignableToSearchRequest(SearchMode mode)
    {
        SearchRequest request = new BasicSearchRequest("hello", mode);

        request.Should().BeOfType<BasicSearchRequest>();
        request.Should().BeAssignableTo<SearchRequest>();
        request.Mode.Should().Be(mode);
        request.Query.Should().Be("hello");
    }
}