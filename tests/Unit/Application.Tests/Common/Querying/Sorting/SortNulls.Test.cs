using Application.Common.Querying.Sorting;

namespace Application.Tests.Common.Querying.Sorting;


public sealed class SortNullsTest
{
    [Theory(DisplayName = "Enum Has Expected Values")]
    [InlineData(SortNulls.None)]
    [InlineData(SortNulls.HasValue)]
    [InlineData(SortNulls.NotHasValue)]
    public void Enum_HasExpectedValues(SortNulls nulls)
    {
        Enum.IsDefined(nulls).Should().BeTrue();
    }

    [Fact(DisplayName = "None Has Value Zero")]
    public void None_HasValueZero()
    {
        ((int)SortNulls.None).Should().Be(0);
    }

    [Fact(DisplayName = "Has Value Has Value One")]
    public void HasValue_HasValueOne()
    {
        ((int)SortNulls.HasValue).Should().Be(1);
    }

    [Fact(DisplayName = "Not Has Value Has Value Two")]
    public void NotHasValue_HasValueTwo()
    {
        ((int)SortNulls.NotHasValue).Should().Be(2);
    }

    [Fact(DisplayName = "Parse Valid String Returns Correct Enum")]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<SortNulls>("None", ignoreCase: true).Should().Be(SortNulls.None);
        Enum.Parse<SortNulls>("HasValue", ignoreCase: true).Should().Be(SortNulls.HasValue);
        Enum.Parse<SortNulls>("NotHasValue", ignoreCase: true).Should().Be(SortNulls.NotHasValue);
    }

    [Fact(DisplayName = "To String Returns Expected Names")]
    public void ToString_ReturnsExpectedNames()
    {
        SortNulls.None.ToString().Should().Be("None");
        SortNulls.HasValue.ToString().Should().Be("HasValue");
        SortNulls.NotHasValue.ToString().Should().Be("NotHasValue");
    }
}