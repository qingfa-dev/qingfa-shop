using Application.Common.Querying;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Parsing;

namespace Application.Unit.Tests.Common.Querying.Parsing;
using Xunit;

public sealed class GroupTextTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Hidden { get; set; } = string.Empty;
    }

    private static QueryMap<Target> Map() =>
        Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b.Groupable())
            .Map("score", x => x.Score, b => b.Groupable())
            .Map("kind", x => x.Kind, b => b.Groupable())
            .Map("hidden", x => x.Hidden, b => b.Groupable(false)));

    private static QueryMap<T> Build<T>(QueryMapBuilder<T> builder)
    {
        Result<QueryMap<T>> map = builder.Build();
        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    private static ParserState<Target> State(QueryMap<Target> map, QueryParseLimits? limits = null) =>
        new(map, limits ?? QueryParseLimits.Default);

    [Fact(DisplayName = "Parse Empty String Returns Empty List")]
    public void Parse_EmptyString_ReturnsEmptyList()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse Null Returns Empty List")]
    public void Parse_Null_ReturnsEmptyList()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse(null, State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse Whitespace Returns Empty List")]
    public void Parse_Whitespace_ReturnsEmptyList()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("   ", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse Single Field Returns Field")]
    public void Parse_SingleField_ReturnsField()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("kind", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().Be(new GroupByField("kind"));
    }

    [Fact(DisplayName = "Parse Multiple Fields Returns All Fields")]
    public void Parse_MultipleFields_ReturnsAllFields()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("kind,name", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Field.Should().Be("kind");
        result.Value[1].Field.Should().Be("name");
    }

    [Fact(DisplayName = "Parse Trims Whitespace Around Fields")]
    public void Parse_TrimsWhitespaceAroundFields()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse(" kind , name ", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Field.Should().Be("kind");
        result.Value[1].Field.Should().Be("name");
    }

    [Fact(DisplayName = "Parse Unknown Field Returns Failure")]
    public void Parse_UnknownField_ReturnsFailure()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("unknown", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Parse Non Groupable Field Returns Failure")]
    public void Parse_NonGroupableField_ReturnsFailure()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("hidden", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }

    [Fact(DisplayName = "Parse Duplicate Field Returns Failure")]
    public void Parse_DuplicateField_ReturnsFailure()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("kind,kind", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.Duplicate");
    }

    [Fact(DisplayName = "Parse Exceeds Max Group Fields Returns Failure")]
    public void Parse_ExceedsMaxGroupFields_ReturnsFailure()
    {
        var limits = new QueryParseLimits { MaxGroupFields = 2 };
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("kind,name,score", State(Map(), limits));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact(DisplayName = "Parse At Max Group Fields Succeeds")]
    public void Parse_AtMaxGroupFields_Succeeds()
    {
        var limits = new QueryParseLimits { MaxGroupFields = 3 };
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("kind,name,score", State(Map(), limits));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Parse Collects All Errors")]
    public void Parse_CollectsAllErrors()
    {
        Result<IReadOnlyList<GroupByField>> result = GroupText.Parse("unknown,hidden", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotGroupable");
    }
}