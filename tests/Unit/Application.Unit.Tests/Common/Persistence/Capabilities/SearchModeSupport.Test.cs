using Application.Common.Persistence.Capabilities;

namespace Application.Unit.Tests.Common.Persistence.Capabilities;

[Trait("Category", "Unit")]
public sealed class SearchModeSupportTest
{
    #region Flag Values

    [Fact(DisplayName = "SearchModeSupport None Should Be Zero")]
    public void None_ShouldBeZero()
    {
        ((int)SearchModeSupport.None).Should().Be(0);
    }

    [Fact(DisplayName = "SearchModeSupport Contains Should Be Power Of Two")]
    public void Contains_ShouldBePowerOfTwo()
    {
        ((int)SearchModeSupport.Contains).Should().Be(1);
    }

    [Fact(DisplayName = "SearchModeSupport StartsWith Should Be Power Of Two")]
    public void StartsWith_ShouldBePowerOfTwo()
    {
        ((int)SearchModeSupport.StartsWith).Should().Be(2);
    }

    [Fact(DisplayName = "SearchModeSupport Exact Should Be Power Of Two")]
    public void Exact_ShouldBePowerOfTwo()
    {
        ((int)SearchModeSupport.Exact).Should().Be(4);
    }

    [Fact(DisplayName = "SearchModeSupport FullText Should Be Power Of Two")]
    public void FullText_ShouldBePowerOfTwo()
    {
        ((int)SearchModeSupport.FullText).Should().Be(8);
    }

    [Fact(DisplayName = "SearchModeSupport Fuzzy Should Be Power Of Two")]
    public void Fuzzy_ShouldBePowerOfTwo()
    {
        ((int)SearchModeSupport.Fuzzy).Should().Be(16);
    }

    #endregion

    #region All Combination

    [Fact(DisplayName = "SearchModeSupport All Should Combine All Modes")]
    public void All_ShouldCombineAllModes()
    {
        var all = SearchModeSupport.All;

        ((all & SearchModeSupport.Contains) != 0).Should().BeTrue();
        ((all & SearchModeSupport.StartsWith) != 0).Should().BeTrue();
        ((all & SearchModeSupport.Exact) != 0).Should().BeTrue();
        ((all & SearchModeSupport.FullText) != 0).Should().BeTrue();
        ((all & SearchModeSupport.Fuzzy) != 0).Should().BeTrue();
    }

    [Fact(DisplayName = "SearchModeSupport All Should Equal Sum Of Individual Flags")]
    public void All_ShouldEqualSumOfIndividualFlags()
    {
        var combined = SearchModeSupport.Contains | SearchModeSupport.StartsWith |
                       SearchModeSupport.Exact | SearchModeSupport.FullText | SearchModeSupport.Fuzzy;

        combined.Should().Be(SearchModeSupport.All);
    }

    #endregion

    #region Bitwise Operations

    [Fact(DisplayName = "SearchModeSupport Should Support Bitwise And For Checking")]
    public void ShouldSupportBitwiseAndForChecking()
    {
        var supported = SearchModeSupport.Contains | SearchModeSupport.Exact;

        ((supported & SearchModeSupport.Contains) != 0).Should().BeTrue();
        ((supported & SearchModeSupport.Exact) != 0).Should().BeTrue();
        ((supported & SearchModeSupport.Fuzzy) != 0).Should().BeFalse();
    }

    [Fact(DisplayName = "SearchModeSupport Should Support Bitwise Or For Combining")]
    public void ShouldSupportBitwiseOrForCombining()
    {
        var combined = SearchModeSupport.Contains | SearchModeSupport.Fuzzy;

        ((combined & SearchModeSupport.Contains) != 0).Should().BeTrue();
        ((combined & SearchModeSupport.Fuzzy) != 0).Should().BeTrue();
        ((combined & SearchModeSupport.Exact) != 0).Should().BeFalse();
    }

    [Fact(DisplayName = "SearchModeSupport Should Support Bitwise Xor For Removing")]
    public void ShouldSupportBitwiseXorForRemoving()
    {
        var all = SearchModeSupport.All;
        var withoutContains = all & ~SearchModeSupport.Contains;

        ((withoutContains & SearchModeSupport.Contains) != 0).Should().BeFalse();
        ((withoutContains & SearchModeSupport.Fuzzy) != 0).Should().BeTrue();
    }

    [Fact(DisplayName = "SearchModeSupport None Should Not Match Any Mode")]
    public void None_ShouldNotMatchAnyMode()
    {
        var none = SearchModeSupport.None;

        ((none & SearchModeSupport.Contains) != 0).Should().BeFalse();
        ((none & SearchModeSupport.StartsWith) != 0).Should().BeFalse();
        ((none & SearchModeSupport.Exact) != 0).Should().BeFalse();
        ((none & SearchModeSupport.FullText) != 0).Should().BeFalse();
        ((none & SearchModeSupport.Fuzzy) != 0).Should().BeFalse();
    }

    #endregion

    #region Enum Attributes

    [Fact(DisplayName = "SearchModeSupport Should Have Flags Attribute")]
    public void ShouldHaveFlagsAttribute()
    {
        var flagsAttribute = typeof(SearchModeSupport)
            .GetCustomAttributes(typeof(FlagsAttribute), false)
            .FirstOrDefault();

        flagsAttribute.Should().NotBeNull();
    }

    [Fact(DisplayName = "SearchModeSupport Should Have Six Values")]
    public void ShouldHaveSixValues()
    {
        var values = Enum.GetValues<SearchModeSupport>().ToList();

        // None, Contains, StartsWith, Exact, FullText, Fuzzy, All = 7
        values.Should().HaveCount(7);
    }

    #endregion

    #region Parsing

    [Theory(DisplayName = "SearchModeSupport Should Parse From String")]
    [InlineData("Contains", SearchModeSupport.Contains)]
    [InlineData("StartsWith", SearchModeSupport.StartsWith)]
    [InlineData("Exact", SearchModeSupport.Exact)]
    [InlineData("FullText", SearchModeSupport.FullText)]
    [InlineData("Fuzzy", SearchModeSupport.Fuzzy)]
    [InlineData("None", SearchModeSupport.None)]
    [InlineData("All", SearchModeSupport.All)]
    public void ShouldParseFromString(string name, SearchModeSupport expected)
    {
        var parsed = (SearchModeSupport)Enum.Parse(typeof(SearchModeSupport), name);
        parsed.Should().Be(expected);
    }

    [Fact(DisplayName = "SearchModeSupport Should Parse Combined Flags")]
    public void ShouldParseCombinedFlags()
    {
        var combined = SearchModeSupport.Contains | SearchModeSupport.Exact;
        var value = (int)combined;

        ((SearchModeSupport)value).Should().Be(combined);
    }

    #endregion
}
