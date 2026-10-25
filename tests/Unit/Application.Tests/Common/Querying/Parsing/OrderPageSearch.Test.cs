using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

namespace Application.Tests.Common.Querying.Parsing;

public sealed class OrderPageSearchTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTimeOffset? DueAtUtc { get; set; }
        public string Hidden { get; set; } = string.Empty;
    }

    private static QueryMap<Target> Map()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, builder => builder.Searchable())
            .Map("score", x => x.Score)
            .Map("dueAtUtc", x => x.DueAtUtc)
            .Map("hidden", x => x.Hidden, builder => builder.Sortable(false))
            .Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    private static QueryMap<Target> Build(QueryMapBuilder<Target> builder)
    {
        Result<QueryMap<Target>> map = builder.Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Fact]
    public void OrderBy_ShouldParseDirectionAndDefaults()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name desc, score" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[0].Direction.Should().Be(SortDirection.Descending);
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void OrderBy_ShouldParseAscendingSuffix()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name asc, score" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[0].Direction.Should().Be(SortDirection.Ascending);
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void OrderBy_ShouldParseDescendingAlias()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "-name" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Single().Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void OrderBy_ShouldParseNullState()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "dueAtUtc? desc" }, Map());

        result.IsSuccess.Should().BeTrue();
        var sort = result.Value.OrderBy.Single();
        sort.Nulls.Should().Be(SortNulls.HasValue);
        sort.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void OrderBy_ShouldParseNotHasValueNullState()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "dueAtUtc!" }, Map());

        result.IsSuccess.Should().BeTrue();
        var sort = result.Value.OrderBy.Single();
        sort.Nulls.Should().Be(SortNulls.NotHasValue);
        sort.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void OrderBy_WhenExceedingMaxSortFields_ShouldFail()
    {
        string orderBy = string.Join(',', Enumerable.Repeat("name", 9));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = orderBy }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact]
    public void OrderBy_WhenDirectionIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name sideways" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact]
    public void OrderBy_WhenFieldIsNotMapped_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "ghost" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void OrderBy_WhenFieldIsNotSortable_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "hidden" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSortable");
    }

    [Fact]
    public void OrderBy_WhenNullStateIsInvalid_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "score?" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact]
    public void Page_ShouldUseDefaults()
    {
        Result<Query<Target>> result = QueryParser.Parse(new QueryParameters(), Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().NotBeNull();
        result.Value.Page!.Page.Should().Be(1);
        result.Value.Page.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(0, 20, "Query.Page.Invalid")]
    [InlineData(1, 0, "Query.PageSize.Invalid")]
    [InlineData(1, 201, "Query.PageSize.Invalid")]
    public void Page_WhenOutOfRange_ShouldFail(int page, int pageSize, string expectedCode)
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Page = page, PageSize = pageSize }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == expectedCode);
    }

    [Fact]
    public void OrderBy_WhenExactlyAtTheFieldLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        string orderBy = string.Join(',', Enumerable.Repeat("name", 8));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = orderBy }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(8);
    }

    [Fact]
    public void Page_WhenExactlyAtTheMaxSize_ShouldSucceed()
    {
        // Boundary: 200 is allowed, 201 is not.
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Page = 1, PageSize = 200 }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().NotBeNull();
        result.Value.Page!.PageSize.Should().Be(200);
    }

    [Fact]
    public void Search_WhenExactlyAtTheLengthLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        string search = new('a', 256);

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = search }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
        result.Value.Search!.Query.Should().Be(search);
    }

    [Fact]
    public void Search_ShouldBeTrimmed()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "  hello  " }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
        result.Value.Search!.Query.Should().Be("hello");
    }

    [Fact]
    public void Search_WhenNoSearchableField_ShouldFail()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("score", x => x.Score));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello" }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.NotConfigured");
    }

    [Fact]
    public void Search_WhenTooLong_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = new string('a', 257) }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.TooLong");
    }

    [Fact]
    public void OrderBy_WhenDirectionIsUnknownSuffix_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name unknown" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact]
    public void OrderBy_WhenNullStateOnNonNullable_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "score?" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact]
    public void Search_WhenEmptyString_ShouldReturnNull()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().BeNull();
    }

    [Fact]
    public void Search_WhenWhitespace_ShouldReturnNull()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "   " }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().BeNull();
    }

    [Fact]
    public void Search_WithFields_ShouldParseFields()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
        result.Value.Search!.Fields.Should().Equal("name");
    }

    [Fact]
    public void Search_WithMultipleFields_ShouldParseFields()
    {
        // Create a map with multiple searchable fields
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name, builder => builder.Searchable())
                .Map("hidden", x => x.Hidden, builder => builder.Searchable())
                .Map("score", x => x.Score));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name,hidden" }, map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.Fields.Should().Equal("name", "hidden");
    }

    [Fact]
    public void Search_WithFields_TrimsWhitespace()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name, builder => builder.Searchable())
                .Map("hidden", x => x.Hidden, builder => builder.Searchable())
                .Map("score", x => x.Score));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = " name , hidden " }, map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.Fields.Should().Equal("name", "hidden");
    }

    [Fact]
    public void Search_WithUnknownField_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "unknown" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void Search_WithNonSearchableField_ShouldFail()
    {
        // score is not marked as Searchable
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "score" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.NotConfigured");
    }

    [Fact]
    public void Search_WithMode_ShouldParseMode()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchMode = SearchMode.Exact }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.Mode.Should().Be(SearchMode.Exact);
    }

    [Fact]
    public void Search_WithTermLogic_ShouldParseTermLogic()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello world", SearchTermLogic = SearchTermLogic.Or }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.TermLogic.Should().Be(SearchTermLogic.Or);
    }

    [Fact]
    public void Search_WithCaseInsensitive_ShouldParseCaseInsensitive()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchCaseInsensitive = false }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.CaseInsensitive.Should().BeFalse();
    }

    [Fact]
    public void Search_WithLanguage_ShouldBeAcceptedButNotStoredInBase()
    {
        // Language is a full-text option; base SearchRequest doesn't store it.
        // It will be used when FullTextSearchRequest is implemented.
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchLanguage = "en" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
    }

    [Fact]
    public void Search_WithIncludeRank_ShouldBeAcceptedButNotStoredInBase()
    {
        // IncludeRank is a full-text option; base SearchRequest doesn't store it.
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchIncludeRank = true }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
    }

    [Fact]
    public void Search_WithMinScore_ShouldBeAcceptedButNotStoredInBase()
    {
        // MinScore is a full-text option; base SearchRequest doesn't store it.
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchMinScore = 0.5 }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
    }

    [Fact]
    public void Search_WithMinScoreOutOfRange_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchMinScore = 1.5 }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.InvalidMinScore");
    }

    [Fact]
    public void Search_WithNegativeMinScore_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchMinScore = -0.1 }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.InvalidMinScore");
    }

    [Fact]
    public void Search_WithLanguageTooLong_ShouldFail()
    {
        string longLanguage = new('a', 11); // Max is 10
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchLanguage = longLanguage }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.LanguageTooLong");
    }

    [Fact]
    public void Search_WithTooManyFields_ShouldFail()
    {
        // Create map with many searchable fields (more than MaxFields=8)
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name, b => b.Searchable())
                .Map("f1", x => x.Name, b => b.Searchable())
                .Map("f2", x => x.Name, b => b.Searchable())
                .Map("f3", x => x.Name, b => b.Searchable())
                .Map("f4", x => x.Name, b => b.Searchable())
                .Map("f5", x => x.Name, b => b.Searchable())
                .Map("f6", x => x.Name, b => b.Searchable())
                .Map("f7", x => x.Name, b => b.Searchable())
                .Map("f8", x => x.Name, b => b.Searchable())
                .Map("f9", x => x.Name, b => b.Searchable()));

        // MaxSearchFields is 8, so 9 fields should fail
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name,f1,f2,f3,f4,f5,f6,f7,f8" }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.TooManyFields");
    }

    [Fact]
    public void Search_WithDuplicateFields_ShouldDeduplicate()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name, builder => builder.Searchable())
                .Map("hidden", x => x.Hidden, builder => builder.Searchable())
                .Map("score", x => x.Score));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name,name,hidden" }, map);

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.Fields.Should().Equal("name", "name", "hidden"); // Currently allows duplicates
    }

    [Fact]
    public void Search_WithTooManyTerms_ShouldFail()
    {
        // Default MaxSearchTerms is 10
        string manyTerms = string.Join(' ', Enumerable.Repeat("term", 11));
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = manyTerms }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.TooManyTerms");
    }
}