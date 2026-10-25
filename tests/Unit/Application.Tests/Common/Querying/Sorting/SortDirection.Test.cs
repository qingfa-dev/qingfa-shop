using Application.Common.Querying.Sorting;

namespace Application.Tests.Common.Querying.Sorting;

public sealed class SortDirectionTest
{
    [Theory]
    [InlineData(SortDirection.Ascending)]
    [InlineData(SortDirection.Descending)]
    public void Enum_HasExpectedValues(SortDirection direction)
    {
        Enum.IsDefined(direction).Should().BeTrue();
    }

    [Fact]
    public void Ascending_HasValueZero()
    {
        ((int)SortDirection.Ascending).Should().Be(0);
    }

    [Fact]
    public void Descending_HasValueOne()
    {
        ((int)SortDirection.Descending).Should().Be(1);
    }

    [Fact]
    public void Parse_ValidString_ReturnsCorrectEnum()
    {
        Enum.Parse<SortDirection>("Ascending", ignoreCase: true).Should().Be(SortDirection.Ascending);
        Enum.Parse<SortDirection>("Descending", ignoreCase: true).Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void ToString_ReturnsExpectedNames()
    {
        SortDirection.Ascending.ToString().Should().Be("Ascending");
        SortDirection.Descending.ToString().Should().Be("Descending");
    }
}