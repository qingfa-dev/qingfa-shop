using Application.Common.Querying;
using Application.Common.Querying.Filtering;

namespace Application.Tests.Common.Querying.Filtering;


public sealed class FilterOperatorTest
{
    [Theory(DisplayName = "Enum Has Expected Values")]
    [InlineData(FilterOperator.Equal)]
    [InlineData(FilterOperator.NotEqual)]
    [InlineData(FilterOperator.LessThan)]
    [InlineData(FilterOperator.GreaterThan)]
    [InlineData(FilterOperator.GreaterThanOrEqual)]
    [InlineData(FilterOperator.LessThanOrEqual)]
    [InlineData(FilterOperator.Contains)]
    [InlineData(FilterOperator.NotContains)]
    [InlineData(FilterOperator.StartsWith)]
    [InlineData(FilterOperator.NotStartsWith)]
    [InlineData(FilterOperator.EndsWith)]
    [InlineData(FilterOperator.NotEndsWith)]
    [InlineData(FilterOperator.Custom)]
    public void Enum_HasExpectedValues(FilterOperator op)
    {
        Enum.IsDefined(op).Should().BeTrue();
    }

    [Fact(DisplayName = "Values Are Sequential")]
    public void Values_AreSequential()
    {
        ((int)FilterOperator.Equal).Should().Be(0);
        ((int)FilterOperator.NotEqual).Should().Be(1);
        ((int)FilterOperator.LessThan).Should().Be(2);
        ((int)FilterOperator.GreaterThan).Should().Be(3);
        ((int)FilterOperator.GreaterThanOrEqual).Should().Be(4);
        ((int)FilterOperator.LessThanOrEqual).Should().Be(5);
        ((int)FilterOperator.Contains).Should().Be(6);
        ((int)FilterOperator.NotContains).Should().Be(7);
        ((int)FilterOperator.StartsWith).Should().Be(8);
        ((int)FilterOperator.NotStartsWith).Should().Be(9);
        ((int)FilterOperator.EndsWith).Should().Be(10);
        ((int)FilterOperator.NotEndsWith).Should().Be(11);
        ((int)FilterOperator.Custom).Should().Be(12);
    }

    [Fact(DisplayName = "Parse Valid String Returns Correct Enum")]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<FilterOperator>("Equal", ignoreCase: true).Should().Be(FilterOperator.Equal);
        Enum.Parse<FilterOperator>("NotEqual", ignoreCase: true).Should().Be(FilterOperator.NotEqual);
        Enum.Parse<FilterOperator>("Custom", ignoreCase: true).Should().Be(FilterOperator.Custom);
    }

    [Fact(DisplayName = "To String Returns Expected Names")]
    public void ToString_ReturnsExpectedNames()
    {
        FilterOperator.Equal.ToString().Should().Be("Equal");
        FilterOperator.NotEqual.ToString().Should().Be("NotEqual");
        FilterOperator.Contains.ToString().Should().Be("Contains");
        FilterOperator.Custom.ToString().Should().Be("Custom");
    }

    [Fact(DisplayName = "Constant Tokens Match Operators")]
    public void ConstantTokens_MatchOperators()
    {
        // Validate: parser tokens match enum for consistency
        QueryConstant.Tokens.Operator.Equal.Should().Be("=");
        QueryConstant.Tokens.Operator.NotEqual.Should().Be("!=");
        QueryConstant.Tokens.Operator.LessThan.Should().Be("<");
        QueryConstant.Tokens.Operator.GreaterThan.Should().Be(">");
        QueryConstant.Tokens.Operator.GreaterThanOrEqual.Should().Be(">=");
        QueryConstant.Tokens.Operator.LessThanOrEqual.Should().Be("<=");
        QueryConstant.Tokens.Operator.Contains.Should().Be("=*");
        QueryConstant.Tokens.Operator.NotContains.Should().Be("!*");
        QueryConstant.Tokens.Operator.StartsWith.Should().Be("^");
        QueryConstant.Tokens.Operator.NotStartsWith.Should().Be("!^");
        QueryConstant.Tokens.Operator.EndsWith.Should().Be("$");
        QueryConstant.Tokens.Operator.NotEndsWith.Should().Be("!$");
        QueryConstant.Tokens.Operator.Custom.Should().Be("#");
    }
}