using Application.Common.Querying;
using Application.Common.Querying.Searching;

namespace Application.Tests.Common.Querying.Searching;

[Trait("Category", "Unit")]
public sealed class FullTextSearchRequestTest
{
    [Fact(DisplayName = "Constructor With Valid Query Sets Properties")]
    public void Constructor_WithValidQuery_SetsProperties()
    {
        var request = new FullTextSearchRequest("hello world");

        request.Query.Should().Be("hello world");
        request.Mode.Should().Be(SearchMode.FullText);
        request.Language.Should().BeNull();
        request.IncludeRank.Should().BeFalse();
        request.MinScore.Should().BeNull();
        request.TermLogic.Should().Be(SearchTermLogic.And); // default from base
        request.CaseInsensitive.Should().BeTrue(); // default from base
        request.Fields.Should().BeNull(); // default from base
    }

    [Fact(DisplayName = "Constructor With All Parameters Sets Properties")]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var request = new FullTextSearchRequest("hello world", "en", true, 0.5);

        request.Query.Should().Be("hello world");
        request.Mode.Should().Be(SearchMode.FullText);
        request.Language.Should().Be("en");
        request.IncludeRank.Should().BeTrue();
        request.MinScore.Should().Be(0.5);
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

    [Fact(DisplayName = "Constructor With Language Exceeding Max Length Throws")]
    public void Constructor_WithLanguageExceedingMaxLength_Throws()
    {
        string longLanguage = new('a', QueryConstant.Limits.Search.MaxLanguageLength + 1);

        Action act = () => new FullTextSearchRequest("query", longLanguage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Language code cannot exceed*")
            .And.ParamName.Should().Be("language");
    }

    [Fact(DisplayName = "Constructor With MinScore Below Zero Throws")]
    public void Constructor_WithMinScoreBelowZero_Throws()
    {
        Action act = () => new FullTextSearchRequest("query", minScore: -0.1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Minimum score must be between 0 and 1*")
            .And.ParamName.Should().Be("minScore");
    }

    [Fact(DisplayName = "Constructor With MinScore Above One Throws")]
    public void Constructor_WithMinScoreAboveOne_Throws()
    {
        Action act = () => new FullTextSearchRequest("query", minScore: 1.1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Minimum score must be between 0 and 1*")
            .And.ParamName.Should().Be("minScore");
    }

    [Theory(DisplayName = "Constructor With MinScore At Boundary Succeeds")]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Constructor_WithMinScoreAtBoundary_Succeeds(double minScore)
    {
        var request = new FullTextSearchRequest("query", minScore: minScore);

        request.MinScore.Should().Be(minScore);
    }

    [Fact(DisplayName = "Constructor With Language At Max Length Succeeds")]
    public void Constructor_WithLanguageAtMaxLength_Succeeds()
    {
        string maxLanguage = new('a', QueryConstant.Limits.Search.MaxLanguageLength);
        var request = new FullTextSearchRequest("query", maxLanguage);

        request.Language.Should().Be(maxLanguage);
    }

    [Theory(DisplayName = "Properties Can Be Set Via Init Accessors")]
    [InlineData("en", true, 0.7)]
    [InlineData("de", false, 0.3)]
    public void Properties_CanBeSetViaInitAccessors(string language, bool includeRank, double minScore)
    {
        var request = new FullTextSearchRequest("query")
        {
            Language = language,
            IncludeRank = includeRank,
            MinScore = minScore
        };

        request.Language.Should().Be(language);
        request.IncludeRank.Should().Be(includeRank);
        request.MinScore.Should().Be(minScore);
    }

    [Fact(DisplayName = "Record Equality Same Values Are Equal")]
    public void RecordEquality_SameValues_AreEqual()
    {
        var r1 = new FullTextSearchRequest("hello", "en", true, 0.5);
        var r2 = new FullTextSearchRequest("hello", "en", true, 0.5);

        r1.Should().Be(r2);
    }

    [Theory(DisplayName = "Record Equality Different Values Are Not Equal")]
    [InlineData("hello", "en", true, 0.5, "world", "en", true, 0.5)]
    [InlineData("hello", "en", true, 0.5, "hello", "de", true, 0.5)]
    [InlineData("hello", "en", true, 0.5, "hello", "en", false, 0.5)]
    [InlineData("hello", "en", true, 0.5, "hello", "en", true, 0.3)]
    public void RecordEquality_DifferentValues_AreNotEqual(
        string query1, string lang1, bool rank1, double minScore1,
        string query2, string lang2, bool rank2, double minScore2)
    {
        var r1 = new FullTextSearchRequest(query1, lang1, rank1, minScore1);
        var r2 = new FullTextSearchRequest(query2, lang2, rank2, minScore2);

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality Different Query Are Not Equal")]
    public void RecordEquality_DifferentQuery_AreNotEqual()
    {
        var r1 = new FullTextSearchRequest("hello", "en");
        var r2 = new FullTextSearchRequest("world", "en");

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality Different Language Are Not Equal")]
    public void RecordEquality_DifferentLanguage_AreNotEqual()
    {
        var r1 = new FullTextSearchRequest("hello", "en");
        var r2 = new FullTextSearchRequest("hello", "de");

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality Different Include Rank Are Not Equal")]
    public void RecordEquality_DifferentIncludeRank_AreNotEqual()
    {
        var r1 = new FullTextSearchRequest("hello", "en", true);
        var r2 = new FullTextSearchRequest("hello", "en", false);

        r1.Should().NotBe(r2);
    }

    [Fact(DisplayName = "Record Equality Different Min Score Are Not Equal")]
    public void RecordEquality_DifferentMinScore_AreNotEqual()
    {
        var r1 = new FullTextSearchRequest("hello", "en", minScore: 0.5);
        var r2 = new FullTextSearchRequest("hello", "en", minScore: 0.3);

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
        SearchRequest request = new FullTextSearchRequest("hello", "en", true, 0.5);

        request.Should().BeOfType<FullTextSearchRequest>();
        request.Should().BeAssignableTo<SearchRequest>();
        request.Mode.Should().Be(SearchMode.FullText);
        request.Query.Should().Be("hello");
    }
}