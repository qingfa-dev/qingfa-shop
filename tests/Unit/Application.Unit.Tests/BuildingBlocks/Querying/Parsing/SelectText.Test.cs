using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Parsing;
using Application.BuildingBlocks.Querying;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Parsing;
using Xunit;

public sealed class SelectTextTest
{
    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Hidden { get; set; } = string.Empty;
    }

    private static QueryMap<Target> Map() =>
        Build(QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name, b => b.Selectable())
            .Map("score", x => x.Score, b => b.Selectable())
            .Map("hidden", x => x.Hidden, b => b.Selectable())
            .Map("internal", x => x.Hidden, b => b.Selectable(false)));

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
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse Null Returns Empty List")]
    public void Parse_Null_ReturnsEmptyList()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse(null, State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse Whitespace Returns Empty List")]
    public void Parse_Whitespace_ReturnsEmptyList()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("   ", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse Single Field Returns Field With Same Alias")]
    public void Parse_SingleField_ReturnsFieldWithSameAlias()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().Be(new SelectField("name", "name"));
    }

    [Fact(DisplayName = "Parse Field With Alias Returns Field With Alias")]
    public void Parse_FieldWithAlias_ReturnsFieldWithAlias()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name as title", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().Be(new SelectField("name", "title"));
    }

    [Fact(DisplayName = "Parse Multiple Fields Returns All Fields")]
    public void Parse_MultipleFields_ReturnsAllFields()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name,score", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Should().Be(new SelectField("name", "name"));
        result.Value[1].Should().Be(new SelectField("score", "score"));
    }

    [Fact(DisplayName = "Parse Multiple Fields With Aliases Returns All With Aliases")]
    public void Parse_MultipleFieldsWithAliases_ReturnsAllWithAliases()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name as title,score as points", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Should().Be(new SelectField("name", "title"));
        result.Value[1].Should().Be(new SelectField("score", "points"));
    }

    [Fact(DisplayName = "Parse Trims Whitespace Around Fields")]
    public void Parse_TrimsWhitespaceAroundFields()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse(" name , score ", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Field.Should().Be("name");
        result.Value[1].Field.Should().Be("score");
    }

    [Fact(DisplayName = "Parse Trims Whitespace Around Alias")]
    public void Parse_TrimsWhitespaceAroundAlias()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name as  title  , score", State(Map()));

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Should().Be(new SelectField("name", "title"));
    }

    [Fact(DisplayName = "Parse Unknown Field Returns Failure")]
    public void Parse_UnknownField_ReturnsFailure()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("unknown", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Parse Non Selectable Field Returns Failure")]
    public void Parse_NonSelectableField_ReturnsFailure()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("internal", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }

    [Fact(DisplayName = "Parse Duplicate Alias Returns Failure")]
    public void Parse_DuplicateAlias_ReturnsFailure()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name as title,score as title", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == "Query.Filter.Syntax" && e.Message.Contains("duplicate alias"));
    }

    [Fact(DisplayName = "Parse Duplicate Field Without Alias Returns Failure")]
    public void Parse_DuplicateFieldWithoutAlias_ReturnsFailure()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name,name", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.Duplicate");
    }

    [Fact(DisplayName = "Parse Duplicate Field With Different Aliases Returns Failure")]
    public void Parse_DuplicateFieldWithDifferentAliases_ReturnsFailure()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name as a, name as b", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.Duplicate");
    }

    [Fact(DisplayName = "Parse Exceeds Max Columns Returns Failure")]
    public void Parse_ExceedsMaxColumns_ReturnsFailure()
    {
        var limits = new QueryParseLimits { MaxSelectFields = 2 };
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name,score,hidden", State(Map(), limits));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.Syntax");
    }

    [Fact(DisplayName = "Parse At Max Columns Succeeds")]
    public void Parse_AtMaxColumns_Succeeds()
    {
        var limits = new QueryParseLimits { MaxSelectFields = 3 };
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("name,score,hidden", State(Map(), limits));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Parse Collects All Errors")]
    public void Parse_CollectsAllErrors()
    {
        Result<IReadOnlyList<SelectField>> result = SelectText.Parse("unknown,internal", State(Map()));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotSelectable");
    }
}