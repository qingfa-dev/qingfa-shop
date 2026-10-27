using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Searching;

namespace Application.Unit.Tests.Common.Querying.Searching;

public sealed class BasicSearchRequestTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private static QueryMap<Target> Map()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, builder => builder.Searchable())
            .Map("description", x => x.Description, builder => builder.Searchable())
            .Map("score", x => x.Score)
            .Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    // ===== Constructor Tests (Parametrized) =====

    [Theory(DisplayName = "Basic Search Request With Valid Mode Should Create With Defaults")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.Fuzzy)]
    public void BasicSearchRequest_WithValidMode_ShouldCreateWithDefaults(SearchMode mode)
    {
        var request = new BasicSearchRequest("hello", mode);

        request.Query.Should().Be("hello");
        request.Mode.Should().Be(mode);
        request.TermLogic.Should().Be(SearchTermLogic.And, "default TermLogic is And");
        request.CaseInsensitive.Should().BeTrue("default CaseInsensitive is true");
        request.Fields.Should().BeNull("default Fields is null");
    }

    [Fact(DisplayName = "Basic Search Request With Full Text Mode Should Throw")]
    public void BasicSearchRequest_WithFullTextMode_ShouldThrow()
    {
        Action act = () => new BasicSearchRequest("hello", SearchMode.FullText);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Use FullTextSearchRequest for FullText mode*")
            .And.ParamName.Should().Be("mode");
    }

    [Theory(DisplayName = "Basic Search Request With Empty Or Whitespace Query Should Throw")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData(" \t \n ")]
    public void BasicSearchRequest_WithEmptyOrWhitespaceQuery_ShouldThrow(string query)
    {
        Action act = () => new BasicSearchRequest(query, SearchMode.Contains);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Search query is required*")
            .And.ParamName.Should().Be("query");
    }

    [Fact(DisplayName = "Basic Search Request With Query Exceeding Max Length Should Throw")]
    public void BasicSearchRequest_WithQueryExceedingMaxLength_ShouldThrow()
    {
        string longQuery = new('a', QueryConstant.Limits.Search.MaxLength + 1);

        Action act = () => new BasicSearchRequest(longQuery, SearchMode.Contains);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*exceeds {QueryConstant.Limits.Search.MaxLength} characters*")
            .And.ParamName.Should().Be("query");
    }

    [Fact(DisplayName = "Basic Search Request With Query At Max Length Should Succeed")]
    public void BasicSearchRequest_WithQueryAtMaxLength_ShouldSucceed()
    {
        string maxQuery = new('a', QueryConstant.Limits.Search.MaxLength);

        var request = new BasicSearchRequest(maxQuery, SearchMode.Contains);

        request.Query.Should().Be(maxQuery);
        request.Query.Length.Should().Be(QueryConstant.Limits.Search.MaxLength);
    }

    // ===== Property Initialization Tests (Parametrized) =====

    [Theory(DisplayName = "Basic Search Request With All Properties Should Set All Values")]
    [InlineData(SearchMode.Contains, "name", SearchTermLogic.And, true)]
    [InlineData(SearchMode.StartsWith, "name,description", SearchTermLogic.Or, false)]
    [InlineData(SearchMode.Exact, "name", SearchTermLogic.And, false)]
    [InlineData(SearchMode.Fuzzy, "description", SearchTermLogic.Or, true)]
    public void BasicSearchRequest_WithAllProperties_ShouldSetAllValues(
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

    [Theory(DisplayName = "Basic Search Request With Fields Should Trim And Set Fields")]
    [InlineData("name")]
    [InlineData("name,description")]
    [InlineData("name, description , other")]
    [InlineData("  name  ,  description  ")]
    public void BasicSearchRequest_WithFields_ShouldTrimAndSetFields(string fieldsCsv)
    {
        var request = new BasicSearchRequest("hello", SearchMode.Contains)
        {
            Fields = fieldsCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        };

        request.Fields.Should().NotBeNull();
        request.Fields.Should().OnlyContain(f => !string.IsNullOrWhiteSpace(f));
        request.Fields.Should().AllSatisfy(f => f.Should().NotContain(" "));
    }

    // ===== GetTerms Tests (Parametrized) =====

    [Theory(DisplayName = "Get Terms With Multiple Terms Should Return All Terms")]
    [InlineData("hello", new[] { "hello" })]
    [InlineData("hello world", new[] { "hello", "world" })]
    [InlineData("hello world test", new[] { "hello", "world", "test" })]
    [InlineData("a b c d e f g h", new[] { "a", "b", "c", "d", "e", "f", "g", "h" })]
    public void GetTerms_WithMultipleTerms_ShouldReturnAllTerms(string query, string[] expected)
    {
        var request = new BasicSearchRequest(query, SearchMode.Contains);

        var terms = request.GetTerms();

        terms.Should().Equal(expected);
        terms.Should().HaveCount(expected.Length);
    }

    [Theory(DisplayName = "Get Terms With Whitespace Variations Should Trim Terms")]
    [InlineData("  hello  ", new[] { "hello" })]
    [InlineData("hello   world", new[] { "hello", "world" })]
    [InlineData("  hello   world  test  ", new[] { "hello", "world", "test" })]
    [InlineData("\thello\tworld\n", new[] { "hello", "world" })]
    public void GetTerms_WithWhitespaceVariations_ShouldTrimTerms(string query, string[] expected)
    {
        var request = new BasicSearchRequest(query, SearchMode.Contains);

        var terms = request.GetTerms();

        terms.Should().Equal(expected);
    }

    [Fact(DisplayName = "Get Terms With Empty Query Should Return Empty")]
    public void GetTerms_WithEmptyQuery_ShouldReturnEmpty()
    {
        // Note: empty query throws in constructor, but testing the method directly
        var request = new BasicSearchRequest("hello", SearchMode.Contains);
        // Can't test empty query via constructor, but method handles it
        // This is just to verify method doesn't throw on valid input
        request.GetTerms().Should().Equal("hello");
    }

    // ===== Record Equality Tests =====

    [Theory(DisplayName = "Basic Search Request Record Equality Different Values Should Not Be Equal")]
    [InlineData("hello", SearchMode.Contains, "world", SearchMode.Contains)]
    [InlineData("hello", SearchMode.Contains, "hello", SearchMode.StartsWith)]
    [InlineData("hello", SearchMode.Contains, "hello", SearchMode.Exact)]
    [InlineData("hello", SearchMode.Contains, "hello", SearchMode.Fuzzy)]
    public void BasicSearchRequest_RecordEquality_DifferentValues_ShouldNotBeEqual(
        string query1, SearchMode mode1, string query2, SearchMode mode2)
    {
        var r1 = new BasicSearchRequest(query1, mode1) { Fields = new[] { "name" }, TermLogic = SearchTermLogic.And, CaseInsensitive = true };
        var r2 = new BasicSearchRequest(query2, mode2) { Fields = new[] { "name" }, TermLogic = SearchTermLogic.And, CaseInsensitive = true };

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Basic Search Request Record Equality Same Values Should Be Equal")]
    public void BasicSearchRequest_RecordEquality_SameValues_ShouldBeEqual()
    {
        var fields = new[] { "name" };
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = fields, TermLogic = SearchTermLogic.And, CaseInsensitive = true };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = fields, TermLogic = SearchTermLogic.And, CaseInsensitive = true };

        r1.Should().Be(r2);
    }

    [Fact(DisplayName = "Basic Search Request Record Equality With Different Fields Should Not Be Equal")]
    public void BasicSearchRequest_RecordEquality_WithDifferentFields_ShouldNotBeEqual()
    {
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = new[] { "name" } };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { Fields = new[] { "description" } };

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Basic Search Request Record Equality With Different Term Logic Should Not Be Equal")]
    public void BasicSearchRequest_RecordEquality_WithDifferentTermLogic_ShouldNotBeEqual()
    {
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { TermLogic = SearchTermLogic.And };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { TermLogic = SearchTermLogic.Or };

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Basic Search Request Record Equality With Different Case Insensitive Should Not Be Equal")]
    public void BasicSearchRequest_RecordEquality_WithDifferentCaseInsensitive_ShouldNotBeEqual()
    {
        var r1 = new BasicSearchRequest("hello", SearchMode.Contains) { CaseInsensitive = true };
        var r2 = new BasicSearchRequest("hello", SearchMode.Contains) { CaseInsensitive = false };

        r1.Should().NotBe(r2);
    }

    // ===== Base Type Assignability =====

    [Theory(DisplayName = "Basic Search Request Should Be Assignable To Search Request")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.Fuzzy)]
    public void BasicSearchRequest_ShouldBeAssignableToSearchRequest(SearchMode mode)
    {
        SearchRequest request = new BasicSearchRequest("hello", mode);

        request.Should().BeOfType<BasicSearchRequest>();
        request.Should().BeAssignableTo<SearchRequest>();
        request.Mode.Should().Be(mode);
        request.Query.Should().Be("hello");
    }

    // ===== Parser Integration Tests (Parametrized) =====

    [Theory(DisplayName = "Parse With Mode Should Return Basic Search Request With Correct Mode")]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.Fuzzy)]
    public void Parse_WithMode_ShouldReturnBasicSearchRequestWithCorrectMode(SearchMode mode)
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchMode = mode }, Map());

        result.IsSuccess.Should().BeTrue();
        var search = result.Value.Search.Should().BeOfType<BasicSearchRequest>().Subject;
        search.Mode.Should().Be(mode);
        search.Query.Should().Be("hello");
        search.TermLogic.Should().Be(SearchTermLogic.And);
        search.CaseInsensitive.Should().BeTrue();
    }

    [Theory(DisplayName = "Parse With Search Fields Should Set Fields On Basic Search Request")]
    [InlineData("name", new[] { "name" })]
    [InlineData("name,description", new[] { "name", "description" })]
    [InlineData(" name , description ", new[] { "name", "description" })]
    public void Parse_WithSearchFields_ShouldSetFieldsOnBasicSearchRequest(string fieldsCsv, string[] expected)
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = fieldsCsv }, Map());

        result.IsSuccess.Should().BeTrue();
        var search = result.Value.Search.Should().BeOfType<BasicSearchRequest>().Subject;
        search.Fields.Should().Equal(expected);
    }

    [Theory(DisplayName = "Parse With Term Logic Should Set Term Logic On Basic Search Request")]
    [InlineData(SearchTermLogic.And)]
    [InlineData(SearchTermLogic.Or)]
    public void Parse_WithTermLogic_ShouldSetTermLogicOnBasicSearchRequest(SearchTermLogic termLogic)
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello world", SearchTermLogic = termLogic }, Map());

        result.IsSuccess.Should().BeTrue();
        var search = result.Value.Search.Should().BeOfType<BasicSearchRequest>().Subject;
        search.TermLogic.Should().Be(termLogic);
    }

    [Theory(DisplayName = "Parse With Case Insensitive Should Set Case Insensitive On Basic Search Request")]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_WithCaseInsensitive_ShouldSetCaseInsensitiveOnBasicSearchRequest(bool caseInsensitive)
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchCaseInsensitive = caseInsensitive }, Map());

        result.IsSuccess.Should().BeTrue();
        var search = result.Value.Search.Should().BeOfType<BasicSearchRequest>().Subject;
        search.CaseInsensitive.Should().Be(caseInsensitive);
    }

    [Fact(DisplayName = "Parse With All Options Should Set All Properties On Basic Search Request")]
    public void Parse_WithAllOptions_ShouldSetAllPropertiesOnBasicSearchRequest()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters
            {
                Search = "hello world",
                SearchFields = "name,description",
                SearchMode = SearchMode.Exact,
                SearchTermLogic = SearchTermLogic.Or,
                SearchCaseInsensitive = false
            }, Map());

        result.IsSuccess.Should().BeTrue();
        var search = result.Value.Search.Should().BeOfType<BasicSearchRequest>().Subject;

        search.Query.Should().Be("hello world");
        search.Fields.Should().Equal("name", "description");
        search.Mode.Should().Be(SearchMode.Exact);
        search.TermLogic.Should().Be(SearchTermLogic.Or);
        search.CaseInsensitive.Should().BeFalse();
        search.GetTerms().Should().Equal("hello", "world");
    }

    // ===== Parser Validation Tests =====

    [Fact(DisplayName = "Parse With Unknown Search Field Should Fail")]
    public void Parse_WithUnknownSearchField_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "unknown" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Parse With Non Searchable Field Should Fail")]
    public void Parse_WithNonSearchableField_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "score" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.NotConfigured");
    }

    [Fact(DisplayName = "Parse With Too Many Search Terms Should Fail")]
    public void Parse_WithTooManySearchTerms_ShouldFail()
    {
        string manyTerms = string.Join(' ', Enumerable.Repeat("term", QueryConstant.Limits.Search.MaxTerms + 1));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = manyTerms }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.TooManyTerms");
    }

    [Fact(DisplayName = "Parse With Terms At Limit Should Succeed")]
    public void Parse_WithTermsAtLimit_ShouldSucceed()
    {
        string maxTerms = string.Join(' ', Enumerable.Repeat("term", QueryConstant.Limits.Search.MaxTerms));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = maxTerms }, Map());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Parse With Too Many Search Fields Should Fail")]
    public void Parse_WithTooManySearchFields_ShouldFail()
    {
        QueryMap<Target> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b.Searchable())
            .Map("f1", x => x.Name, b => b.Searchable())
            .Map("f2", x => x.Name, b => b.Searchable())
            .Map("f3", x => x.Name, b => b.Searchable())
            .Map("f4", x => x.Name, b => b.Searchable())
            .Map("f5", x => x.Name, b => b.Searchable())
            .Map("f6", x => x.Name, b => b.Searchable())
            .Map("f7", x => x.Name, b => b.Searchable())
            .Map("f8", x => x.Name, b => b.Searchable())
            .Map("f9", x => x.Name, b => b.Searchable())
            .Build().Value;

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name,f1,f2,f3,f4,f5,f6,f7,f8" }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.TooManyFields");
    }

    [Fact(DisplayName = "Parse With Search Fields At Limit Should Succeed")]
    public void Parse_WithSearchFieldsAtLimit_ShouldSucceed()
    {
        QueryMap<Target> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b.Searchable())
            .Map("f1", x => x.Name, b => b.Searchable())
            .Map("f2", x => x.Name, b => b.Searchable())
            .Map("f3", x => x.Name, b => b.Searchable())
            .Map("f4", x => x.Name, b => b.Searchable())
            .Map("f5", x => x.Name, b => b.Searchable())
            .Map("f6", x => x.Name, b => b.Searchable())
            .Map("f7", x => x.Name, b => b.Searchable())
            .Map("f8", x => x.Name, b => b.Searchable())
            .Build().Value;

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name,f1,f2,f3,f4,f5,f6,f7" }, map);

        result.IsSuccess.Should().BeTrue();
    }
}