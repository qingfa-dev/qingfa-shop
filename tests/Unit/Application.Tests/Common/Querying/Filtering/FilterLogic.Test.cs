using Application.Common.Querying;
using Application.Common.Querying.Filtering;

namespace Application.Tests.Common.Querying.Filtering;

public sealed class FilterLogicTest
{
    [Theory]
    [InlineData(FilterLogic.And)]
    [InlineData(FilterLogic.Or)]
    public void Enum_HasExpectedValues(FilterLogic logic)
    {
        Enum.IsDefined(logic).Should().BeTrue();
    }

    [Fact]
    public void And_HasValueZero()
    {
        ((int)FilterLogic.And).Should().Be(0);
    }

    [Fact]
    public void Or_HasValueOne()
    {
        ((int)FilterLogic.Or).Should().Be(1);
    }

    [Fact]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<FilterLogic>("And", ignoreCase: true).Should().Be(FilterLogic.And);
        Enum.Parse<FilterLogic>("Or", ignoreCase: true).Should().Be(FilterLogic.Or);
    }

    [Fact]
    public void ToString_ReturnsExpectedNames()
    {
        FilterLogic.And.ToString().Should().Be("And");
        FilterLogic.Or.ToString().Should().Be("Or");
    }

    [Fact]
    public void ConstantCharacters_MatchLogic()
    {
        // Validate: parser characters match enum for consistency
        QueryConstant.Tokens.Character.And.Should().Be(',');
        QueryConstant.Tokens.Character.Or.Should().Be('|');
    }
}