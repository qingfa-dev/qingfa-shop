using Application.BuildingBlocks.Persistence.Capabilities;

namespace Application.Unit.Tests.BuildingBlocks.Persistence.Capabilities;

[Trait("Category", "Unit")]
public sealed class QueryCapabilityConstantTest
{
    #region Filter Limits

    [Fact(DisplayName = "Filter MaxLength Should Be 2048")]
    public void Filter_MaxLength_ShouldBe2048()
    {
        QueryCapabilityConstant.Filter.MaxLength.Should().Be(2048);
    }

    [Fact(DisplayName = "Filter MaxDepth Should Be 32")]
    public void Filter_MaxDepth_ShouldBe32()
    {
        QueryCapabilityConstant.Filter.MaxDepth.Should().Be(32);
    }

    [Fact(DisplayName = "Filter MaxConditions Should Be 100")]
    public void Filter_MaxConditions_ShouldBe100()
    {
        QueryCapabilityConstant.Filter.MaxConditions.Should().Be(100);
    }

    #endregion

    #region Order Limits

    [Fact(DisplayName = "Order MaxFields Should Be 8")]
    public void Order_MaxFields_ShouldBe8()
    {
        QueryCapabilityConstant.Order.MaxFields.Should().Be(8);
    }

    #endregion

    #region Select Limits

    [Fact(DisplayName = "Select MaxColumns Should Be 32")]
    public void Select_MaxColumns_ShouldBe32()
    {
        QueryCapabilityConstant.Select.MaxColumns.Should().Be(32);
    }

    #endregion

    #region Group Limits

    [Fact(DisplayName = "Group MaxKeys Should Be 8")]
    public void Group_MaxKeys_ShouldBe8()
    {
        QueryCapabilityConstant.Group.MaxKeys.Should().Be(8);
    }

    #endregion

    #region Aggregate Limits

    [Fact(DisplayName = "Aggregate MaxCount Should Be 16")]
    public void Aggregate_MaxCount_ShouldBe16()
    {
        QueryCapabilityConstant.Aggregate.MaxCount.Should().Be(16);
    }

    #endregion

    #region Search Limits

    [Fact(DisplayName = "Search MaxLength Should Be 256")]
    public void Search_MaxLength_ShouldBe256()
    {
        QueryCapabilityConstant.Search.MaxLength.Should().Be(256);
    }

    [Fact(DisplayName = "Search MaxTerms Should Be 8")]
    public void Search_MaxTerms_ShouldBe8()
    {
        QueryCapabilityConstant.Search.MaxTerms.Should().Be(8);
    }

    [Fact(DisplayName = "Search MaxFields Should Be 8")]
    public void Search_MaxFields_ShouldBe8()
    {
        QueryCapabilityConstant.Search.MaxFields.Should().Be(8);
    }

    #endregion

    #region Map Limits

    [Fact(DisplayName = "Map MaxFieldNameLength Should Be 64")]
    public void Map_MaxFieldNameLength_ShouldBe64()
    {
        QueryCapabilityConstant.Map.MaxFieldNameLength.Should().Be(64);
    }

    #endregion

    #region Tokens - Operators

    [Theory(DisplayName = "Operator Constants Should Have Expected Values")]
    [InlineData("Equal", "=")]
    [InlineData("NotEqual", "!=")]
    [InlineData("LessThan", "<")]
    [InlineData("GreaterThan", ">")]
    [InlineData("GreaterThanOrEqual", ">=")]
    [InlineData("LessThanOrEqual", "<=")]
    [InlineData("Contains", "=*")]
    [InlineData("NotContains", "!*")]
    [InlineData("StartsWith", "^")]
    [InlineData("NotStartsWith", "!^")]
    [InlineData("EndsWith", "$")]
    [InlineData("NotEndsWith", "!$")]
    [InlineData("Custom", "#")]
    public void Operator_ShouldHaveExpectedValue(string operatorName, string expectedValue)
    {
        var field = typeof(QueryCapabilityConstant.Tokens.Operator)
            .GetField(operatorName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull($"Operator.{operatorName} should exist");
        field!.GetValue(null).Should().Be(expectedValue);
    }

    [Fact(DisplayName = "TwoCharacterOperators Should Contain All Two-Char Operators")]
    public void TwoCharacterOperators_ShouldContainAllTwoCharOperators()
    {
        var operators = QueryCapabilityConstant.Tokens.TwoCharacterOperators;

        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.NotEqual);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.GreaterThanOrEqual);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.LessThanOrEqual);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.Contains);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.NotContains);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.NotStartsWith);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.NotEndsWith);
        operators.Should().HaveCount(7);
    }

    [Fact(DisplayName = "OneCharacterOperators Should Contain All One-Char Operators")]
    public void OneCharacterOperators_ShouldContainAllOneCharOperators()
    {
        var operators = QueryCapabilityConstant.Tokens.OneCharacterOperators;

        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.Equal);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.LessThan);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.GreaterThan);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.StartsWith);
        operators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.EndsWith);
        operators.Should().HaveCount(5);
    }

    #endregion

    #region Tokens - Characters

    [Theory(DisplayName = "Character Constants Should Have Expected Values")]
    [InlineData("And", ',')]
    [InlineData("Or", '|')]
    [InlineData("GroupOpen", '(')]
    [InlineData("GroupClose", ')')]
    [InlineData("Escape", '\\')]
    [InlineData("CaseInsensitive", '/')]
    [InlineData("CaseInsensitiveMarker", 'i')]
    [InlineData("Space", ' ')]
    [InlineData("Tab", '\t')]
    public void Character_ShouldHaveExpectedValue(string characterName, char expectedValue)
    {
        var field = typeof(QueryCapabilityConstant.Tokens.Character)
            .GetField(characterName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull($"Character.{characterName} should exist");
        field!.GetValue(null).Should().Be(expectedValue);
    }

    #endregion

    #region Tokens - Keywords

    [Theory(DisplayName = "Keyword Constants Should Have Expected Values")]
    [InlineData("Ascending", "asc")]
    [InlineData("Descending", "desc")]
    [InlineData("Null", "null")]
    [InlineData("CaseInsensitive", "/i")]
    [InlineData("Rank", "__rank")]
    public void Keyword_ShouldHaveExpectedValue(string keywordName, string expectedValue)
    {
        var field = typeof(QueryCapabilityConstant.Tokens.Keyword)
            .GetField(keywordName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull($"Keyword.{keywordName} should exist");
        field!.GetValue(null).Should().Be(expectedValue);
    }

    #endregion

    #region Tokens - SearchMode

    [Theory(DisplayName = "SearchMode Constants Should Have Expected Values")]
    [InlineData("Contains", "contains")]
    [InlineData("StartsWith", "startswith")]
    [InlineData("Exact", "exact")]
    [InlineData("FullText", "fulltext")]
    [InlineData("Fuzzy", "fuzzy")]
    public void SearchMode_ShouldHaveExpectedValue(string modeName, string expectedValue)
    {
        var field = typeof(QueryCapabilityConstant.Tokens.SearchMode)
            .GetField(modeName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull($"SearchMode.{modeName} should exist");
        field!.GetValue(null).Should().Be(expectedValue);
    }

    #endregion

    #region Tokens - Separators

    [Theory(DisplayName = "Separator Constants Should Have Expected Values")]
    [InlineData("Alias", " as ")]
    [InlineData("Member", '.')]
    public void Separator_ShouldHaveExpectedValue(string separatorName, object expectedValue)
    {
        var field = typeof(QueryCapabilityConstant.Tokens.Separator)
            .GetField(separatorName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull($"Separator.{separatorName} should exist");
        field!.GetValue(null).Should().Be(expectedValue);
    }

    #endregion

    #region Tokens - FieldTerminators

    [Fact(DisplayName = "FieldTerminators Should Include All Operator And Structural Characters")]
    public void FieldTerminators_ShouldIncludeAllOperatorAndStructuralCharacters()
    {
        var terminators = QueryCapabilityConstant.Tokens.FieldTerminators;

        // Should include all operator first characters
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.Equal[0]);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.NotEqual[0]);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.LessThan[0]);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.GreaterThan[0]);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.StartsWith[0]);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.EndsWith[0]);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Operator.Custom[0]);

        // Should include structural characters
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Character.And);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Character.Or);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Character.GroupOpen);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Character.GroupClose);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Character.Space);
        terminators.Should().Contain(QueryCapabilityConstant.Tokens.Character.Tab);
    }

    #endregion

    #region Functions

    [Theory(DisplayName = "Function Constants Should Have Expected Values")]
    [InlineData("Sum", "sum")]
    [InlineData("Average", "avg")]
    [InlineData("Minimum", "min")]
    [InlineData("Maximum", "max")]
    [InlineData("Count", "count")]
    [InlineData("DistinctCount", "distinctcount")]
    public void Function_ShouldHaveExpectedValue(string functionName, string expectedValue)
    {
        var field = typeof(QueryCapabilityConstant.Functions)
            .GetField(functionName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull($"Functions.{functionName} should exist");
        field!.GetValue(null).Should().Be(expectedValue);
    }

    [Fact(DisplayName = "AggregatePattern Should Match Valid Aggregate Expressions")]
    public void AggregatePattern_ShouldMatchValidAggregateExpressions()
    {
        var pattern = QueryCapabilityConstant.Functions.AggregatePattern;
        var regex = new System.Text.RegularExpressions.Regex(pattern);

        regex.IsMatch("sum(price)").Should().BeTrue();
        regex.IsMatch("avg(quantity)").Should().BeTrue();
        regex.IsMatch("min(date)").Should().BeTrue();
        regex.IsMatch("max(score)").Should().BeTrue();
        regex.IsMatch("count(id)").Should().BeTrue();
        regex.IsMatch("distinctcount(email)").Should().BeTrue();
    }

    [Fact(DisplayName = "AggregatePattern Should Be Case Sensitive For Function Names")]
    public void AggregatePattern_ShouldBeCaseSensitiveForFunctionNames()
    {
        var pattern = QueryCapabilityConstant.Functions.AggregatePattern;
        var regex = new System.Text.RegularExpressions.Regex(pattern);

        // The regex is case-sensitive for function names
        regex.IsMatch("SUM(Price)").Should().BeFalse();
        regex.IsMatch("AVG(Quantity)").Should().BeFalse();
    }

    [Fact(DisplayName = "AggregatePattern Should Reject Invalid Expressions")]
    public void AggregatePattern_ShouldRejectInvalidExpressions()
    {
        var pattern = QueryCapabilityConstant.Functions.AggregatePattern;
        var regex = new System.Text.RegularExpressions.Regex(pattern);

        regex.IsMatch("invalid(price)").Should().BeFalse(); // Invalid function
        regex.IsMatch("sum").Should().BeFalse(); // Missing parentheses
        regex.IsMatch("sum()").Should().BeFalse(); // Empty field
        regex.IsMatch("sum(price").Should().BeFalse(); // Missing closing paren
        regex.IsMatch("sum price)").Should().BeFalse(); // Missing opening paren
    }

    #endregion
}