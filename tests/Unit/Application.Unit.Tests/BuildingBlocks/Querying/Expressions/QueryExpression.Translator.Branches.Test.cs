using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Expressions;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Expressions;

/// <summary>
/// Exercises the translator's typed failure and negation branches so unsupported
/// expressions fail loudly instead of silently producing a weaker query.
/// </summary>
public sealed class QueryExpressionTranslatorBranchesTest
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Score { get; set; }
        public bool Active { get; set; }
        public bool Flag { get; set; }
        public string Locked { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Ambiguous { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private sealed class GroupSpec
    {
        public string? First { get; set; }
        public int Second { get; set; }
    }

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("name", item => item.Name, field => field.Searchable().Groupable())
            .Map("description", item => item.Description, field => field.Searchable())
            .Map("score", item => item.Score, field => field.Groupable())
            .Map("active", item => item.Active, field => field.Groupable())
            .Map("flag", item => item.Flag, field => field.Filterable(false))
            .Map("locked", item => item.Locked, field => field.Filterable(false))
            .Map("secret", item => item.Secret, field => field.Selectable(false))
            .Map("code", item => item.Code, field => field.Sortable(false).Groupable(false))
            .Map("amb1", item => item.Ambiguous, field => field.MemberPath("Ambiguous"))
            .Map("amb2", item => item.Ambiguous, field => field.MemberPath("Ambiguous"))
            .Build().Value;

    // ---- Predicate branches -------------------------------------------

    [Fact(DisplayName = "Comparison on a non-filterable field should fail typed")]
    public void TranslatePredicate_NotFilterableComparison_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Locked == "x", CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotFilterable);
    }

    [Fact(DisplayName = "Non-filterable boolean member should fail typed")]
    public void TranslatePredicate_NotFilterableBooleanMember_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Flag, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotFilterable);
    }

    [Fact(DisplayName = "Negated non-filterable boolean member should fail typed")]
    public void TranslatePredicate_NotFilterableNegatedBoolean_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !item.Flag, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotFilterable);
    }

    [Fact(DisplayName = "String method on a non-filterable field should fail typed")]
    public void TranslatePredicate_NotFilterableStringMethod_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Locked.StartsWith("x"), CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotFilterable);
    }

    [Fact(DisplayName = "StartsWith should map to its operator")]
    public void TranslatePredicate_WithStartsWith_ShouldReturnOperator()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.StartsWith("a"), CreateMap());

        result.Value.Should().BeOfType<FilterCondition>()
            .Which.Operator.Should().Be(FilterOperator.StartsWith);
    }

    [Fact(DisplayName = "EndsWith should map to its operator")]
    public void TranslatePredicate_WithEndsWith_ShouldReturnOperator()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.EndsWith("z"), CreateMap());

        result.Value.Should().BeOfType<FilterCondition>()
            .Which.Operator.Should().Be(FilterOperator.EndsWith);
    }

    [Fact(DisplayName = "Unsupported string method should fail typed")]
    public void TranslatePredicate_UnsupportedStringMethod_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Name.Equals("x"), CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryOutcome.Codes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Unsupported predicate node should fail typed")]
    public void TranslatePredicate_UnsupportedNode_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Active ? true : false, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryOutcome.Codes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "Member on the right should flip the relational operator")]
    public void TranslatePredicate_MemberOnRight_ShouldFlipOperator()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => 5 < item.Score, CreateMap());

        var condition = result.Value.Should().BeOfType<FilterCondition>().Subject;
        condition.Field.Should().Be("score");
        condition.Operator.Should().Be(FilterOperator.GreaterThan);
        condition.Value.Should().Be(5);
    }

    [Fact(DisplayName = "Negated comparison should negate the operator")]
    public void TranslatePredicate_NegatedComparison_ShouldNegateOperator()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !(item.Score > 5), CreateMap());

        result.Value.Should().BeOfType<FilterCondition>()
            .Which.Operator.Should().Be(FilterOperator.LessThanOrEqual);
    }

    [Fact(DisplayName = "Negated string method should negate the operator")]
    public void TranslatePredicate_NegatedStringMethod_ShouldNegateOperator()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !item.Name.Contains("x"), CreateMap());

        result.Value.Should().BeOfType<FilterCondition>()
            .Which.Operator.Should().Be(FilterOperator.NotContains);
    }

    [Fact(DisplayName = "Negated group should invert the logic and every child")]
    public void TranslatePredicate_NegatedGroup_ShouldInvertLogic()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !(item.Active && item.Score > 5), CreateMap());

        var group = result.Value.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.Or);
        group.Children.OfType<FilterCondition>().Should().Contain(condition =>
            condition.Field == "active" && condition.Operator == FilterOperator.NotEqual);
        group.Children.OfType<FilterCondition>().Should().Contain(condition =>
            condition.Field == "score" && condition.Operator == FilterOperator.LessThanOrEqual);
    }

    [Fact(DisplayName = "A failing left operand should fail the group")]
    public void TranslatePredicate_LeftOperandFails_ShouldReturnFailure()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Tags.Contains("x") && item.Active, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryOutcome.Codes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "A failing right operand should fail the group")]
    public void TranslatePredicate_RightOperandFails_ShouldReturnFailure()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Active && item.Tags.Contains("x"), CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryOutcome.Codes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "A comparison without a constant operand should fail typed")]
    public void TranslatePredicate_BothMembers_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Score > item.Score, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(
            error => error.Code == QueryOutcome.Codes.BuilderExpressionUnsupported);
    }

    [Fact(DisplayName = "A negated failing operand should preserve the failure")]
    public void TranslatePredicate_NegatedFailingOperand_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => !item.Tags.Contains("x"), CreateMap());

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "An ambiguous member path should fail typed")]
    public void TranslatePredicate_AmbiguousMember_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslatePredicate(
            item => item.Ambiguous == 1, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.BuilderAmbiguousField);
    }

    // ---- Ordering branches --------------------------------------------

    [Fact(DisplayName = "Order expression should reject a non-sortable field")]
    public void TranslateOrder_NotSortable_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateOrder(
            item => item.Code, SortDirection.Ascending, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotSortable);
    }

    [Fact(DisplayName = "Order expression should reject an unmapped path")]
    public void TranslateOrder_Unmapped_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateOrder(
            item => item.Tags.Count, SortDirection.Ascending, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
    }

    // ---- Select branches ----------------------------------------------

    [Fact(DisplayName = "Select expression should reject a non-selectable field")]
    public void TranslateSelect_NotSelectable_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateSelect(
            item => item.Secret, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotSelectable);
    }

    [Fact(DisplayName = "Select expression should reject an unmapped path")]
    public void TranslateSelect_Unmapped_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateSelect(
            item => item.Tags.Count, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
    }

    // ---- Grouping branches --------------------------------------------

    [Fact(DisplayName = "Group expression should expand a member-init object")]
    public void TranslateGroup_MemberInit_ShouldReturnKeys()
    {
        var result = QueryExpressionTranslator<Item>.TranslateGroup(
            item => new GroupSpec { First = item.Name, Second = item.Score }, CreateMap());

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(key => key.Field).Should().BeEquivalentTo(["name", "score"]);
    }

    [Fact(DisplayName = "Group expression should reject a duplicate key")]
    public void TranslateGroup_DuplicateKey_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateGroup(
            item => new { First = item.Name, Second = item.Name }, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldDuplicate);
    }

    [Fact(DisplayName = "Group expression should reject a non-groupable field")]
    public void TranslateGroup_NotGroupable_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateGroup(
            item => item.Description, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotGroupable);
    }

    [Fact(DisplayName = "Group expression should reject an unmapped path")]
    public void TranslateGroup_Unmapped_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateGroup(
            item => item.Tags.Count, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
    }

    // ---- Aggregate branches -------------------------------------------

    [Fact(DisplayName = "Aggregate expression should reject a non-selectable field")]
    public void TranslateAggregate_NotSelectable_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateAggregate(
            AggregateFunction.Count, item => item.Secret, null, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotSelectable);
    }

    [Fact(DisplayName = "Aggregate expression should keep a caller supplied alias")]
    public void TranslateAggregate_WithAlias_ShouldUseAlias()
    {
        var result = QueryExpressionTranslator<Item>.TranslateAggregate(
            AggregateFunction.Count, item => item.Score, "total", CreateMap());

        result.Value.Should().Be(new AggregateSpec(AggregateFunction.Count, "score", "total"));
    }

    [Fact(DisplayName = "Aggregate expression should reject an unmapped path")]
    public void TranslateAggregate_Unmapped_ShouldFail()
    {
        var result = QueryExpressionTranslator<Item>.TranslateAggregate(
            AggregateFunction.Count, item => item.Tags.Count, null, CreateMap());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == QueryOutcome.Codes.FieldNotMapped);
    }
}
