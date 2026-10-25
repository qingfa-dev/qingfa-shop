using Core.Common.Querying;
using Core.Common.Querying.Mapping;

namespace Core.Tests.Common.Querying.Parsing;

public sealed class FilterLimitTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public List<string> Tags { get; set; } = [];
        public Target? Child { get; set; }
    }

    private static QueryMap<Target> Map()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name)
            .Map("description", x => x.Description)
            .Map("score", x => x.Score)
            .Map("child.name", x => x.Child!.Name)
            .Map("tags", x => x.Tags)
            .Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    private static Result<Query<Target>> Parse(string filter, QueryOptions? options = null) =>
        QueryParser.Parse(new QueryParameters { Filter = filter }, Map(), options);

    [Fact]
    public void Filter_WhenDepthIsAtLimit_ShouldSucceed()
    {
        string filter = string.Concat(Enumerable.Repeat("(", 32)) + "name=a" +
            string.Concat(Enumerable.Repeat(")", 32));

        Result<Query<Target>> result = Parse(filter);

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<Core.Common.Querying.Filtering.FilterCondition>()
            .Which.Value.Should().Be("a");
    }

    [Fact]
    public void Filter_WhenDepthExceedsLimit_ShouldFail()
    {
        string filter = string.Concat(Enumerable.Repeat("(", 33)) + "name=a" +
            string.Concat(Enumerable.Repeat(")", 33));

        Result<Query<Target>> result = Parse(filter);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.DepthExceeded");
    }

    [Fact]
    public void Filter_WhenTooDeep_ShouldFail()
    {
        string filter = string.Concat(Enumerable.Repeat("(", 40)) + "name=a" +
            string.Concat(Enumerable.Repeat(")", 40));

        Result<Query<Target>> result = Parse(filter);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.DepthExceeded");
    }

    [Fact]
    public void Filter_WhenTooManyConditions_ShouldFail()
    {
        string filter = string.Join(',', Enumerable.Repeat("name=a", 101));

        Result<Query<Target>> result = Parse(filter);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.TooManyConditions");
    }

    [Fact]
    public void Filter_WhenLongerThanTheLimit_ShouldFail()
    {
        string filter = new('a', 2049);

        Result<Query<Target>> result = Parse(filter);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.TooLong");
    }

    [Fact]
    public void Filter_WhenExactlyAtTheLengthLimit_ShouldSucceed()
    {
        // Boundary: the limit is inclusive. An off-by-one here would reject a
        // legitimate filter or admit one that is over the limit.
        string filter = "name=" + new string('a', 2043);

        Result<Query<Target>> result = Parse(filter);

        filter.Length.Should().Be(2048);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Filter_WhenExactlyAtTheConditionLimit_ShouldSucceed()
    {
        string filter = string.Join(',', Enumerable.Repeat("name=a", 100));

        Result<Query<Target>> result = Parse(filter);

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<Core.Common.Querying.Filtering.FilterGroup>();
    }
}
