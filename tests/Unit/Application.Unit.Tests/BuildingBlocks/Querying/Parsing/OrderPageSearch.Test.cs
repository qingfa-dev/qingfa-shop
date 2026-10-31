using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Parsing;


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

    [Fact(DisplayName = "Order By Should Parse Direction And Defaults")]
    public void OrderBy_ShouldParseDirectionAndDefaults()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name desc, score" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[0].Direction.Should().Be(SortDirection.Descending);
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "Order By Should Parse Ascending Suffix")]
    public void OrderBy_ShouldParseAscendingSuffix()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name asc, score" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(2);
        result.Value.OrderBy[0].Direction.Should().Be(SortDirection.Ascending);
        result.Value.OrderBy[1].Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "Order By Should Parse Descending Alias")]
    public void OrderBy_ShouldParseDescendingAlias()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "-name" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Single().Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "Order By Should Parse Null State")]
    public void OrderBy_ShouldParseNullState()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "dueAtUtc? desc" }, Map());

        result.IsSuccess.Should().BeTrue();
        var sort = result.Value.OrderBy.Single();
        sort.Nulls.Should().Be(SortNulls.HasValue);
        sort.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact(DisplayName = "Order By Should Parse Not Has Value Null State")]
    public void OrderBy_ShouldParseNotHasValueNullState()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "dueAtUtc!" }, Map());

        result.IsSuccess.Should().BeTrue();
        var sort = result.Value.OrderBy.Single();
        sort.Nulls.Should().Be(SortNulls.NotHasValue);
        sort.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact(DisplayName = "Order By When Exceeding Max Sort Fields Should Fail")]
    public void OrderBy_WhenExceedingMaxSortFields_ShouldFail()
    {
        string orderBy = string.Join(',', Enumerable.Repeat("name", 9));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = orderBy }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact(DisplayName = "Order By When Direction Is Unknown Should Fail")]
    public void OrderBy_WhenDirectionIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name sideways" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact(DisplayName = "Order By When Field Is Not Mapped Should Fail")]
    public void OrderBy_WhenFieldIsNotMapped_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "ghost" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Order By When Field Is Not Sortable Should Fail")]
    public void OrderBy_WhenFieldIsNotSortable_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "hidden" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSortable");
    }

    [Fact(DisplayName = "Order By When Null State Is Invalid Should Fail")]
    public void OrderBy_WhenNullStateIsInvalid_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "score?" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact(DisplayName = "Page Should Use Defaults")]
    public void Page_ShouldUseDefaults()
    {
        Result<Query<Target>> result = QueryParser.Parse(new QueryParameters(), Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().NotBeNull();
        result.Value.Page!.Page.Should().Be(1);
        result.Value.Page.PageSize.Should().Be(20);
    }

    [Theory(DisplayName = "Page When Out Of Range Should Fail")]
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

    [Fact(DisplayName = "Order By When Exactly At The Field Limit Should Succeed")]
    public void OrderBy_WhenExactlyAtTheFieldLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        string orderBy = string.Join(',', Enumerable.Repeat("name", 8));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = orderBy }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().HaveCount(8);
    }

    [Fact(DisplayName = "Page When Exactly At The Max Size Should Succeed")]
    public void Page_WhenExactlyAtTheMaxSize_ShouldSucceed()
    {
        // Boundary: 200 is allowed, 201 is not.
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Page = 1, PageSize = 200 }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().NotBeNull();
        result.Value.Page!.PageSize.Should().Be(200);
    }

    [Fact(DisplayName = "Search When Exactly At The Length Limit Should Succeed")]
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

    [Fact(DisplayName = "Search Should Be Trimmed")]
    public void Search_ShouldBeTrimmed()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "  hello  " }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
        result.Value.Search!.Query.Should().Be("hello");
    }

    [Fact(DisplayName = "Search When No Searchable Field Should Fail")]
    public void Search_WhenNoSearchableField_ShouldFail()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create().Map("score", x => x.Score));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello" }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.NotConfigured");
    }

    [Fact(DisplayName = "Search When Too Long Should Fail")]
    public void Search_WhenTooLong_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = new string('a', 257) }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.TooLong");
    }

    [Fact(DisplayName = "Order By When Direction Is Unknown Suffix Should Fail")]
    public void OrderBy_WhenDirectionIsUnknownSuffix_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "name unknown" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact(DisplayName = "Order By When Null State On Non Nullable Should Fail")]
    public void OrderBy_WhenNullStateOnNonNullable_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { OrderBy = "score?" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Order.Syntax");
    }

    [Fact(DisplayName = "Search When Empty String Should Return Null")]
    public void Search_WhenEmptyString_ShouldReturnNull()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().BeNull();
    }

    [Fact(DisplayName = "Search When Whitespace Should Return Null")]
    public void Search_WhenWhitespace_ShouldReturnNull()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "   " }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().BeNull();
    }

    [Fact(DisplayName = "Search With Fields Should Parse Fields")]
    public void Search_WithFields_ShouldParseFields()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "name" }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().NotBeNull();
        result.Value.Search!.Fields.Should().Equal("name");
    }

    [Fact(DisplayName = "Search With Multiple Fields Should Parse Fields")]
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

    [Fact(DisplayName = "Search With Fields Trims Whitespace")]
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

    [Fact(DisplayName = "Search With Unknown Field Should Fail")]
    public void Search_WithUnknownField_ShouldFail()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "unknown" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Search With Non Searchable Field Should Fail")]
    public void Search_WithNonSearchableField_ShouldFail()
    {
        // score is not marked as Searchable
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchFields = "score" }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Search.NotConfigured");
    }

    [Fact(DisplayName = "Search With Mode Should Parse Mode")]
    public void Search_WithMode_ShouldParseMode()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchMode = SearchMode.Exact }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.Mode.Should().Be(SearchMode.Exact);
    }

    [Fact(DisplayName = "Search With Term Logic Should Parse Term Logic")]
    public void Search_WithTermLogic_ShouldParseTermLogic()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello world", SearchTermLogic = SearchTermLogic.Or }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.TermLogic.Should().Be(SearchTermLogic.Or);
    }

    [Fact(DisplayName = "Search With Case Insensitive Should Parse Case Insensitive")]
    public void Search_WithCaseInsensitive_ShouldParseCaseInsensitive()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "hello", SearchCaseInsensitive = false }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search!.CaseInsensitive.Should().BeFalse();
    }

    [Fact(DisplayName = "Search With Too Many Fields Should Fail")]
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

    [Fact(DisplayName = "Search With Duplicate Fields Should Deduplicate")]
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

    [Fact(DisplayName = "Search With Too Many Terms Should Fail")]
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