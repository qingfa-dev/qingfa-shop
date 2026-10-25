using Application.Common.Querying.Searching;

namespace Application.Tests.Common.Querying.Searching;


public sealed class SearchTermLogicTest
{
    [Theory(DisplayName = "Enum Has Expected Values")]
    [InlineData(SearchTermLogic.And)]
    [InlineData(SearchTermLogic.Or)]
    public void Enum_HasExpectedValues(SearchTermLogic logic)
    {
        Enum.IsDefined(logic).Should().BeTrue();
    }

    [Fact(DisplayName = "And Has Value Zero")]
    public void And_HasValueZero()
    {
        ((int)SearchTermLogic.And).Should().Be(0);
    }

    [Fact(DisplayName = "Or Has Value One")]
    public void Or_HasValueOne()
    {
        ((int)SearchTermLogic.Or).Should().Be(1);
    }

    [Fact(DisplayName = "Parse Valid String Returns Correct Enum")]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<SearchTermLogic>("And", ignoreCase: true).Should().Be(SearchTermLogic.And);
        Enum.Parse<SearchTermLogic>("Or", ignoreCase: true).Should().Be(SearchTermLogic.Or);
    }

    [Fact(DisplayName = "To String Returns Expected Names")]
    public void ToString_ReturnsExpectedNames()
    {
        SearchTermLogic.And.ToString().Should().Be("And");
        SearchTermLogic.Or.ToString().Should().Be("Or");
    }
}