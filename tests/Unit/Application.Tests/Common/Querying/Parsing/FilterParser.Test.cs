using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;

namespace Application.Tests.Common.Querying.Parsing;


public sealed class FilterParserTest
{
    private enum Kind { Low, High }

    private sealed class Target
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public Kind Kind { get; set; }
        public List<string> Tags { get; set; } = [];
        public Target? Child { get; set; }
    }

    private static QueryMap<Target> Map()
    {
        Result<QueryMap<Target>> map = QueryMapBuilder<Target>.Create()
            .Map("name", x => x.Name)
            .Map("description", x => x.Description)
            .Map("score", x => x.Score)
            .Map("kind", x => x.Kind)
            .Map("child.name", x => x.Child!.Name)
            .Map("tags", x => x.Tags)
            .MapIndexed<int>("tag", (target, index) => target.Tags[index])
            .MapComposite("any", [x => x.Name, x => x.Description!])
            .Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    private static Result<Query<Target>> Parse(string filter, QueryParseLimits? options = null) =>
        QueryParser.Parse(new QueryParameters { Filter = filter }, Map(), options);

    private static QueryMap<Target> Build(QueryMapBuilder<Target> builder)
    {
        Result<QueryMap<Target>> map = builder.Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Theory(DisplayName = "Filter Should Parse String Operators")]
    [InlineData("name=John", FilterOperator.Equal, "John")]
    [InlineData("name!=John", FilterOperator.NotEqual, "John")]
    [InlineData("name=*ohn", FilterOperator.Contains, "ohn")]
    [InlineData("name!*ohn", FilterOperator.NotContains, "ohn")]
    [InlineData("name^Jo", FilterOperator.StartsWith, "Jo")]
    [InlineData("name!^Jo", FilterOperator.NotStartsWith, "Jo")]
    [InlineData("name$hn", FilterOperator.EndsWith, "hn")]
    [InlineData("name!$hn", FilterOperator.NotEndsWith, "hn")]
    public void Filter_ShouldParseStringOperators(string filter, FilterOperator expected, string value)
    {
        Result<Query<Target>> result = Parse(filter);

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(expected);
        condition.Value.Should().Be(value);
    }

    [Theory(DisplayName = "Filter Should Parse Comparison Operators")]
    [InlineData("score>10", FilterOperator.GreaterThan, 10)]
    [InlineData("score<10", FilterOperator.LessThan, 10)]
    [InlineData("score>=10", FilterOperator.GreaterThanOrEqual, 10)]
    [InlineData("score<=10", FilterOperator.LessThanOrEqual, 10)]
    public void Filter_ShouldParseComparisonOperators(string filter, FilterOperator expected, int value)
    {
        Result<Query<Target>> result = Parse(filter);

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(expected);
        condition.Value.Should().Be(value);
    }

    [Fact(DisplayName = "Filter Should Apply And Before Or")]
    public void Filter_ShouldApplyAndBeforeOr()
    {
        Result<Query<Target>> result = Parse("name=*a|name=*b,score>1");

        result.IsSuccess.Should().BeTrue();
        var root = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        root.Logic.Should().Be(FilterLogic.Or);
        root.Children.Should().HaveCount(2);
        root.Children[1].Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.And);
    }

    [Fact(DisplayName = "Filter Should Respect Parentheses")]
    public void Filter_ShouldRespectParentheses()
    {
        Result<Query<Target>> result = Parse("(name=*a|name=*b),score>1");

        result.IsSuccess.Should().BeTrue();
        var root = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        root.Logic.Should().Be(FilterLogic.And);
        root.Children[0].Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "Filter Should Unescape Reserved Characters")]
    public void Filter_ShouldUnescapeReservedCharacters()
    {
        Result<Query<Target>> result = Parse(@"name=*John\,Doe");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be("John,Doe");
    }

    [Fact(DisplayName = "Filter Should Parse Case Insensitive Suffix")]
    public void Filter_ShouldParseCaseInsensitiveSuffix()
    {
        Result<Query<Target>> result = Parse("name=*JOHN/i");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.CaseInsensitive.Should().BeTrue();
        condition.Value.Should().Be("JOHN");
    }

    [Fact(DisplayName = "Filter Should Treat Empty Value As Null Or Default")]
    public void Filter_ShouldTreatEmptyValueAsNullOrDefault()
    {
        Result<Query<Target>> result = Parse("description=");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.RawValue.Should().BeEmpty();
        condition.Value.Should().BeNull();
    }

    [Fact(DisplayName = "Filter Should Convert Empty Numeric Value To Default")]
    public void Filter_ShouldConvertEmptyNumericValueToDefault()
    {
        Result<Query<Target>> result = Parse("score=");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.RawValue.Should().BeEmpty();
        condition.Value.Should().Be(0);
    }

    [Fact(DisplayName = "Filter Should Convert Empty Enum Value To Default")]
    public void Filter_ShouldConvertEmptyEnumValueToDefault()
    {
        Result<Query<Target>> result = Parse("kind=");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be(Kind.Low);
    }

    [Fact(DisplayName = "Filter When Value Is Empty Should Invoke Custom Converter")]
    public void Filter_WhenValueIsEmpty_ShouldInvokeCustomConverter()
    {
        bool invoked = false;
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map(
                    "score",
                    x => x.Score,
                    builder => builder.ConvertWith(raw =>
                    {
                        invoked = true;
                        return Result<QueryValue>.Success(new QueryValue(7));
                    })));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Filter = "score=" }, map);

        result.IsSuccess.Should().BeTrue();
        invoked.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be(7);
    }

    [Fact(DisplayName = "Filter Should Parse Explicit Null")]
    public void Filter_ShouldParseExplicitNull()
    {
        Result<Query<Target>> result = Parse("description=null");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().BeNull();
    }

    [Fact(DisplayName = "Filter Should Convert Enum By Name")]
    public void Filter_ShouldConvertEnumByName()
    {
        Result<Query<Target>> result = Parse("kind=high");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be(Kind.High);
    }

    [Fact(DisplayName = "Filter Should Parse Indexer")]
    public void Filter_ShouldParseIndexer()
    {
        Result<Query<Target>> result = Parse("tag[0]=urgent");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.IndexValue.Should().Be(0);
        condition.Value.Should().Be("urgent");
    }

    [Fact(DisplayName = "Filter Should Parse Composite Field")]
    public void Filter_ShouldParseCompositeField()
    {
        Result<Query<Target>> result = Parse("any=*foo");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Field.Should().Be("any");
    }

    [Fact(DisplayName = "Filter Should Parse Custom Operator")]
    public void Filter_ShouldParseCustomOperator()
    {
        Result<Query<Target>> result = Parse("name#In John;David");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(FilterOperator.Custom);
        condition.CustomOperator.Should().Be("In");
        condition.Value.Should().Be("John;David");
    }

    [Fact(DisplayName = "Filter When Field Is Unknown Should Fail")]
    public void Filter_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse("unknown=1");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact(DisplayName = "Filter When Value Cannot Convert Should Fail")]
    public void Filter_WhenValueCannotConvert_ShouldFail()
    {
        Result<Query<Target>> result = Parse("kind=wrong");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.ValueInvalid");
    }

    [Fact(DisplayName = "Filter When Operator Does Not Fit Type Should Fail")]
    public void Filter_WhenOperatorDoesNotFitType_ShouldFail()
    {
        Result<Query<Target>> result = Parse("score=*text");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.TypeMismatch");
    }

    [Theory(DisplayName = "Filter When The Syntax Is Malformed Should Fail")]
    [InlineData("(name=a", "missing ')'")]
    [InlineData("=a", "field is missing")]
    [InlineData("name", "operator is missing")]
    [InlineData("name#", "unknown operator")]
    [InlineData("name=a)", "unexpected trailing text")]
    public void Filter_WhenTheSyntaxIsMalformed_ShouldFail(string filter, string expectedDetail)
    {
        Result<Query<Target>> result = Parse(filter);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == "Query.Filter.Syntax" && e.Message.Contains(expectedDetail));
    }

    [Fact(DisplayName = "Filter When The Field Is Not Filterable Should Fail")]
    public void Filter_WhenTheFieldIsNotFilterable_ShouldFail()
    {
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name, field => field.Filterable(false)));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Filter = "name=a" }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "Filter When Several Parts Are Wrong Should Report Every Problem")]
    public void Filter_WhenSeveralPartsAreWrong_ShouldReportEveryProblem()
    {
        // The parser parses every part independently, so a caller learns about all
        // the problems in one round-trip instead of one per attempt.
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters
            {
                Filter = "missing=a",
                OrderBy = "missing",
            }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Count(e => e.Code == "Query.Field.NotMapped").Should().Be(2);
    }

    [Fact(DisplayName = "Filter When Several Tabular Parts Are Wrong Should Report Every Problem")]
    public void Filter_WhenSeveralTabularPartsAreWrong_ShouldReportEveryProblem()
    {
        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters
            {
                Select = "missing",
                GroupBy = "alsomissing",
            }, Map());

        result.IsFailure.Should().BeTrue();
        result.Errors.Count(e => e.Code == "Query.Field.NotMapped").Should().Be(2);
    }

    [Fact(DisplayName = "Filter When Empty String Should Return Null")]
    public void Filter_WhenEmptyString_ShouldReturnNull()
    {
        Result<Query<Target>> result = Parse("");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeNull();
    }

    [Fact(DisplayName = "Filter When Whitespace Should Return Null")]
    public void Filter_WhenWhitespace_ShouldReturnNull()
    {
        Result<Query<Target>> result = Parse("   ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeNull();
    }

    [Fact(DisplayName = "Filter Text When Empty Should Return Empty Group")]
    public void FilterText_WhenEmpty_ShouldReturnEmptyGroup()
    {
        // Direct call to FilterText.Parse to cover the empty filter branch
        // that the public QueryParser.Parse skips.
        var map = Map();
        var state = new Application.Common.Querying.Parsing.ParserState<Target>(map, QueryParseLimits.Default);
        Result<FilterNode> result = Application.Common.Querying.Parsing.FilterText.Parse("", state);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<FilterGroup>()
            .Which.Children.Should().BeEmpty();
    }

    [Fact(DisplayName = "Filter Text When Whitespace Should Return Empty Group")]
    public void FilterText_WhenWhitespace_ShouldReturnEmptyGroup()
    {
        var map = Map();
        var state = new Application.Common.Querying.Parsing.ParserState<Target>(map, QueryParseLimits.Default);
        Result<FilterNode> result = Application.Common.Querying.Parsing.FilterText.Parse("   ", state);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<FilterGroup>()
            .Which.Children.Should().BeEmpty();
    }

    [Theory(DisplayName = "Filter When Indexer Used On Non Indexed Field Should Fail")]
    [InlineData("tags[0]=urgent")]
    [InlineData("name[0]=test")]
    public void Filter_WhenIndexerUsedOnNonIndexedField_ShouldFail(string filter)
    {
        // Create a map where the field is NOT indexed
        QueryMap<Target> map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name)
                .Map("score", x => x.Score)
                .Map("tags", x => x.Tags)  // This is a collection but not indexed
                .Map("kind", x => x.Kind));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Filter = filter }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "Filter With Case Insensitive On String Should Set Flag")]
    public void Filter_WithCaseInsensitiveOnString_ShouldSetFlag()
    {
        Result<Query<Target>> result = Parse("name=*JOHN/i");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.CaseInsensitive.Should().BeTrue();
        condition.Value.Should().Be("JOHN");
    }

    [Fact(DisplayName = "Filter With Case Insensitive On Non String Should Fail")]
    public void Filter_WithCaseInsensitiveOnNonString_ShouldFail()
    {
        Result<Query<Target>> result = Parse("score=*10/i");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.TypeMismatch");
    }

    [Fact(DisplayName = "Filter With Allow Null Search Disabled Should Fail On Null")]
    public void Filter_WithAllowNullSearchDisabled_ShouldFailOnNull()
    {
        var map = Build(
            QueryMapBuilder<Target>.Create()
                .Map("name", x => x.Name, b => b.AllowNullSearch(false)));

        Result<Query<Target>> result = QueryParser.Parse(
            new QueryParameters { Filter = "name=null" }, map);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.ValueInvalid");
    }

    [Fact(DisplayName = "Filter With Escaped Value Should Unescape")]
    public void Filter_WithEscapedValue_ShouldUnescape()
    {
        Result<Query<Target>> result = Parse(@"name=hello\,world");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Value.Should().Be("hello,world");
    }

    [Fact(DisplayName = "Filter With Multiple Or Groups Should Parse Correctly")]
    public void Filter_WithMultipleOrGroups_ShouldParseCorrectly()
    {
        Result<Query<Target>> result = Parse("name=a|name=b|name=c");

        result.IsSuccess.Should().BeTrue();
        var root = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        root.Logic.Should().Be(FilterLogic.Or);
        root.Children.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Filter With Nested Parentheses Should Parse Correctly")]
    public void Filter_WithNestedParentheses_ShouldParseCorrectly()
    {
        Result<Query<Target>> result = Parse("((name=a))");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Value.Should().Be("a");
    }
}
