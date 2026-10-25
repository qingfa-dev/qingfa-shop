using Application.Common.Querying.Sorting;

namespace Application.Tests.Common.Querying.Sorting;

public sealed class SortNullsTest
{
    [Theory]
    [InlineData(SortNulls.None)]
    [InlineData(SortNulls.HasValue)]
    [InlineData(SortNulls.NotHasValue)]
    public void Enum_HasExpectedValues(SortNulls nulls)
    {
        Enum.IsDefined(nulls).Should().BeTrue();
    }

    [Fact]
    public void None_HasValueZero()
    {
        ((int)SortNulls.None).Should().Be(0);
    }

    [Fact]
    public void HasValue_HasValueOne()
    {
        ((int)SortNulls.HasValue).Should().Be(1);
    }

    [Fact]
    public void NotHasValue_HasValueTwo()
    {
        ((int)SortNulls.NotHasValue).Should().Be(2);
    }

    [Fact]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<SortNulls>("None", ignoreCase: true).Should().Be(SortNulls.None);
        Enum.Parse<SortNulls>("HasValue", ignoreCase: true).Should().Be(SortNulls.HasValue);
        Enum.Parse<SortNulls>("NotHasValue", ignoreCase: true).Should().Be(SortNulls.NotHasValue);
    }

    [Fact]
    public void ToString_ReturnsExpectedNames()
    {
        SortNulls.None.ToString().Should().Be("None");
        SortNulls.HasValue.ToString().Should().Be("HasValue");
        SortNulls.NotHasValue.ToString().Should().Be("NotHasValue");
    }
}