using Application.Common.Querying.Searching;

namespace Application.Tests.Common.Querying.Searching;

public sealed class SearchTermLogicTest
{
    [Theory]
    [InlineData(SearchTermLogic.And)]
    [InlineData(SearchTermLogic.Or)]
    public void Enum_HasExpectedValues(SearchTermLogic logic)
    {
        Enum.IsDefined(logic).Should().BeTrue();
    }

    [Fact]
    public void And_HasValueZero()
    {
        ((int)SearchTermLogic.And).Should().Be(0);
    }

    [Fact]
    public void Or_HasValueOne()
    {
        ((int)SearchTermLogic.Or).Should().Be(1);
    }

    [Fact]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<SearchTermLogic>("And", ignoreCase: true).Should().Be(SearchTermLogic.And);
        Enum.Parse<SearchTermLogic>("Or", ignoreCase: true).Should().Be(SearchTermLogic.Or);
    }

    [Fact]
    public void ToString_ReturnsExpectedNames()
    {
        SearchTermLogic.And.ToString().Should().Be("And");
        SearchTermLogic.Or.ToString().Should().Be("Or");
    }
}