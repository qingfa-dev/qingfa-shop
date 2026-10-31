using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Expressions;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Expressions;

public sealed class QueryExpressionTranslatorTest
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public bool Active { get; set; }
        public bool IsCompleted { get; set; }
        public ItemKind Kind { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private string Name { get; } = "captured";

    private bool IsCompleted { get; set; } = true;

    private static string Score => "static";

    private enum ItemKind
    {
        First,
        Second,
    }

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("name", item => item.Name, field => field.Searchable())
            .Map("description", item => item.Description, field => field.Searchable())
            .Map("score", item => item.Score)
            .Map("active", item => item.Active, field => field.Groupable())
            .Map("isCompleted", item => item.IsCompleted, field => field.Groupable())
            .Map("kind", item => item.Kind, field => field.Groupable())
            .Map("tags", item => item.Tags)
            .Build().Value;

    [Fact(DisplayName = "Where expression should map equality")]
    public void TranslatePredicate_WithEquality_ShouldReturnCondition()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name == "test", CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<FilterCondition>()
            .Which.Field.Should().Be("name");
    }

    [Fact(DisplayName = "Where expression should combine with AND")]
    public void TranslatePredicate_WithAnd_ShouldReturnGroup()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Active && item.Score > 5, CreateMap());

        result.IsSuccess.Should().BeTrue();
        var group = result.Value.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.And);
        group.Children.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Where expression should support null checks")]
    public void TranslatePredicate_WithNull_ShouldReturnNullCondition()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Description == null, CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(FilterOperator.Equal);
        condition.Value.Should().BeNull();
    }

    [Fact(DisplayName = "Where expression should support string methods")]
    public void TranslatePredicate_WithContains_ShouldReturnContains()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.Contains("test"), CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<FilterCondition>()
            .Which.Operator.Should().Be(FilterOperator.Contains);
    }

    [Fact(DisplayName = "Where expression should support negated boolean members")]
    public void TranslatePredicate_WithNegatedBool_ShouldReturnFalseCondition()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !item.Active, CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("active");
        condition.Value.Should().Be(false);
    }

    [Fact(DisplayName = "Where expression should reject an unmapped member")]
    public void TranslatePredicate_WithUnmappedMember_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Description!.Length > 2, CreateMap());

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "Where expression should reject a non-string Contains")]
    public void TranslatePredicate_WithCollectionContains_ShouldFailTyped()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Tags.Contains("sale"), CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Where expression should reject a computed value operand")]
    public void TranslatePredicate_WithComputedValueOperand_ShouldFailTyped()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Score > item.Score * 2, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Where expression should preserve double negation on a boolean member")]
    public void TranslatePredicate_WithDoubleNegatedBool_ShouldReturnTrueCondition()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !!item.Active, CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("active");
        condition.Operator.Should().Be(FilterOperator.Equal);
        condition.Value.Should().Be(true);
    }

    [Fact(DisplayName = "Where expression should preserve double negation on a comparison")]
    public void TranslatePredicate_WithDoubleNegatedComparison_ShouldReturnOriginalCondition()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !!(item.Score > 5), CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("score");
        condition.Operator.Should().Be(FilterOperator.GreaterThan);
        condition.Value.Should().Be(5);
    }

    [Fact(DisplayName = "Where expression should invert triple negation on a boolean member")]
    public void TranslatePredicate_WithTripleNegatedBool_ShouldReturnFalseCondition()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !!!item.Active, CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("active");
        condition.Operator.Should().Be(FilterOperator.Equal);
        condition.Value.Should().Be(false);
    }

    [Fact(DisplayName = "Where expression should preserve a group under double negation")]
    public void TranslatePredicate_WithDoubleNegatedGroup_ShouldPreserveAnd()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !!(item.Active && item.Score > 5), CreateMap());

        result.IsSuccess.Should().BeTrue();
        var group = result.Value.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.And);
        group.Children.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Where expression should reject a captured bool that collides with a mapped field")]
    public void TranslatePredicate_WithCapturedBoolColliding_ShouldFailTyped()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => IsCompleted, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Where expression should reject a captured string receiver that collides with a mapped field")]
    public void TranslatePredicate_WithCapturedStringReceiver_ShouldFailTyped()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => Name.Contains("test"), CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Order expression should reject a static property that collides with a mapped field")]
    public void TranslateOrder_WithStaticProperty_ShouldFailTyped()
    {
        var result = QueryExpressionTranslator<Item>.TranslateOrder(
            item => Score, SortDirection.Ascending, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryErrorCodes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Where expression should evaluate a captured Contains value")]
    public void TranslatePredicate_WithCapturedContains_ShouldReturnContains()
    {
        string term = "test";

        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.Contains(term), CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(FilterOperator.Contains);
        condition.Value.Should().Be(term);
    }

    [Fact(DisplayName = "Where expression should evaluate a captured StartsWith value")]
    public void TranslatePredicate_WithCapturedStartsWith_ShouldReturnStartsWith()
    {
        string prefix = "prefix";

        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.StartsWith(prefix), CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(FilterOperator.StartsWith);
        condition.Value.Should().Be(prefix);
    }

    [Fact(DisplayName = "Where expression should evaluate a captured EndsWith value")]
    public void TranslatePredicate_WithCapturedEndsWith_ShouldReturnEndsWith()
    {
        string suffix = "suffix";

        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.EndsWith(suffix), CreateMap());

        result.IsSuccess.Should().BeTrue();
        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Operator.Should().Be(FilterOperator.EndsWith);
        condition.Value.Should().Be(suffix);
    }
}