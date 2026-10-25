using Core.Common.Querying;
using Core.Common.Querying.Filtering;
using Core.Common.Querying.Mapping;

namespace Core.Tests.Common.Querying.Parsing;

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

    private static Result<Query<Target>> Parse(string filter, QueryOptions? options = null) =>
        QueryParser.Parse(new QueryParameters { Filter = filter }, Map(), options);

    private static QueryMap<Target> Build(QueryMapBuilder<Target> builder)
    {
        Result<QueryMap<Target>> map = builder.Build();

        map.IsSuccess.Should().BeTrue();
        return map.Value;
    }

    [Theory]
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

    [Theory]
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

    [Fact]
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

    [Fact]
    public void Filter_ShouldRespectParentheses()
    {
        Result<Query<Target>> result = Parse("(name=*a|name=*b),score>1");

        result.IsSuccess.Should().BeTrue();
        var root = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        root.Logic.Should().Be(FilterLogic.And);
        root.Children[0].Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact]
    public void Filter_ShouldUnescapeReservedCharacters()
    {
        Result<Query<Target>> result = Parse(@"name=*John\,Doe");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be("John,Doe");
    }

    [Fact]
    public void Filter_ShouldParseCaseInsensitiveSuffix()
    {
        Result<Query<Target>> result = Parse("name=*JOHN/i");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.CaseInsensitive.Should().BeTrue();
        condition.Value.Should().Be("JOHN");
    }

    [Fact]
    public void Filter_ShouldTreatEmptyValueAsNullOrDefault()
    {
        Result<Query<Target>> result = Parse("description=");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.RawValue.Should().BeEmpty();
        condition.Value.Should().BeNull();
    }

    [Fact]
    public void Filter_ShouldConvertEmptyNumericValueToDefault()
    {
        Result<Query<Target>> result = Parse("score=");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.RawValue.Should().BeEmpty();
        condition.Value.Should().Be(0);
    }

    [Fact]
    public void Filter_ShouldConvertEmptyEnumValueToDefault()
    {
        Result<Query<Target>> result = Parse("kind=");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be(Kind.Low);
    }

    [Fact]
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

    [Fact]
    public void Filter_ShouldParseExplicitNull()
    {
        Result<Query<Target>> result = Parse("description=null");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().BeNull();
    }

    [Fact]
    public void Filter_ShouldConvertEnumByName()
    {
        Result<Query<Target>> result = Parse("kind=high");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Value.Should().Be(Kind.High);
    }

    [Fact]
    public void Filter_ShouldParseIndexer()
    {
        Result<Query<Target>> result = Parse("tag[0]=urgent");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.IndexValue.Should().Be(0);
        condition.Value.Should().Be("urgent");
    }

    [Fact]
    public void Filter_ShouldParseCompositeField()
    {
        Result<Query<Target>> result = Parse("any=*foo");

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
            .Which.Field.Should().Be("any");
    }

    [Fact]
    public void Filter_ShouldParseCustomOperator()
    {
        Result<Query<Target>> result = Parse("name#In John;David");

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(FilterOperator.Custom);
        condition.CustomOperator.Should().Be("In");
        condition.Value.Should().Be("John;David");
    }

    [Fact]
    public void Filter_WhenFieldIsUnknown_ShouldFail()
    {
        Result<Query<Target>> result = Parse("unknown=1");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotMapped");
    }

    [Fact]
    public void Filter_WhenValueCannotConvert_ShouldFail()
    {
        Result<Query<Target>> result = Parse("kind=wrong");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Filter.ValueInvalid");
    }

    [Fact]
    public void Filter_WhenOperatorDoesNotFitType_ShouldFail()
    {
        Result<Query<Target>> result = Parse("score=*text");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Operator.TypeMismatch");
    }

    [Fact]
    public void Filter_WhenDepthIsAtLimit_ShouldSucceed()
    {
        string filter = string.Concat(Enumerable.Repeat("(", 32)) + "name=a" +
            string.Concat(Enumerable.Repeat(")", 32));

        Result<Query<Target>> result = Parse(filter);

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeOfType<FilterCondition>()
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
        result.Value.Filter.Should().BeOfType<FilterGroup>();
    }

    [Theory]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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
}