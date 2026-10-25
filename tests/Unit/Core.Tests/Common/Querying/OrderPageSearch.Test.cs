using Core.Common.Querying;
using Core.Common.Querying.Mapping;
using Core.Common.Querying.Sorting;

namespace Core.Tests.Common.Querying;

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
        result.Value.Page.Page.Should().Be(1);
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
        result.Value.Page.PageSize.Should().Be(200);
    }

    [Fact]
    public void Search_WhenExactlyAtTheLengthLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive.
        string search = new('a', 256);

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = search }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().Be(search);
    }

    [Fact]
    public void Search_ShouldBeTrimmed()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Search = "  hello  " }, Map());

        result.IsSuccess.Should().BeTrue();
        result.Value.Search.Should().Be("hello");
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
}