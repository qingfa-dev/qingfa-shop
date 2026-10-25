using Application.Common.Querying;
using Application.Common.Querying.Searching;

namespace Application.Tests.Common.Querying.Searching;

public sealed class SearchModeTest
{
    [Theory]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.StartsWith)]
    [InlineData(SearchMode.Exact)]
    [InlineData(SearchMode.FullText)]
    [InlineData(SearchMode.Fuzzy)]
    public void Enum_HasExpectedValues(SearchMode mode)
    {
        Enum.IsDefined(mode).Should().BeTrue();
    }

    [Fact]
    public void Contains_HasValueZero()
    {
        ((int)SearchMode.Contains).Should().Be(0);
    }

    [Fact]
    public void StartsWith_HasValueOne()
    {
        ((int)SearchMode.StartsWith).Should().Be(1);
    }

    [Fact]
    public void Exact_HasValueTwo()
    {
        ((int)SearchMode.Exact).Should().Be(2);
    }

    [Fact]
    public void FullText_HasValueThree()
    {
        ((int)SearchMode.FullText).Should().Be(3);
    }

    [Fact]
    public void Fuzzy_HasValueFour()
    {
        ((int)SearchMode.Fuzzy).Should().Be(4);
    }

    [Fact]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<SearchMode>("Contains", ignoreCase: true).Should().Be(SearchMode.Contains);
        Enum.Parse<SearchMode>("StartsWith", ignoreCase: true).Should().Be(SearchMode.StartsWith);
        Enum.Parse<SearchMode>("Exact", ignoreCase: true).Should().Be(SearchMode.Exact);
        Enum.Parse<SearchMode>("FullText", ignoreCase: true).Should().Be(SearchMode.FullText);
        Enum.Parse<SearchMode>("Fuzzy", ignoreCase: true).Should().Be(SearchMode.Fuzzy);
    }

    [Fact]
    public void ToString_ReturnsExpectedNames()
    {
        SearchMode.Contains.ToString().Should().Be("Contains");
        SearchMode.StartsWith.ToString().Should().Be("StartsWith");
        SearchMode.Exact.ToString().Should().Be("Exact");
        SearchMode.FullText.ToString().Should().Be("FullText");
        SearchMode.Fuzzy.ToString().Should().Be("Fuzzy");
    }

    [Fact]
    public void ConstantTokens_MatchEnumNames()
    {
        // Validate: the grammar tokens match the enum for parser consistency
        QueryConstant.Tokens.SearchMode.Contains.Should().Be("contains");
        QueryConstant.Tokens.SearchMode.StartsWith.Should().Be("startswith");
        QueryConstant.Tokens.SearchMode.Exact.Should().Be("exact");
        QueryConstant.Tokens.SearchMode.FullText.Should().Be("fulltext");
        QueryConstant.Tokens.SearchMode.Fuzzy.Should().Be("fuzzy");
    }
}