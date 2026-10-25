using Core.Common.Domain.ValueObjects.Times;

namespace Core.Tests.Domain.ValueObjects.Times;

public class DateRangeExtensionSpec
{
    [Fact]
    public void Duration_TwoDayRange_ShouldReturnTwo()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2)).Value;

        // Act:
        var duration = range.Duration();

        // Assert:
        duration.Should().Be(2);
    }

    [Fact]
    public void Duration_SingleDayRange_ShouldReturnOne()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1)).Value;

        // Act:
        var duration = range.Duration();

        // Assert:
        duration.Should().Be(1);
    }

    [Fact]
    public void Includes_DateInsideRange_ShouldReturnTrue()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 3)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 2));

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Includes_StartEndpoint_ShouldReturnTrue()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 3)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 1));

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Includes_EndEndpoint_ShouldReturnTrue()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 3)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 3));

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Includes_DateBeforeRange_ShouldReturnFalse()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 3)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 1));

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Includes_DateAfterRange_ShouldReturnFalse()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 3));

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Includes_SingleDayRangeMatchingDate_ShouldReturnTrue()
    {
        // Arrange:
        var date = new DateOnly(2026, 1, 2);
        var range = DateRange.Create(date, date).Value;

        // Act:
        var result = range.Includes(date);

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Includes_SingleDayRangeDayBefore_ShouldReturnFalse()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 2)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 1));

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Includes_SingleDayRangeDayAfter_ShouldReturnFalse()
    {
        // Arrange:
        var range = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 2)).Value;

        // Act:
        var result = range.Includes(new DateOnly(2026, 1, 3));

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ShouldReturnTrue()
    {
        // Arrange:
        var left = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 3)).Value;
        var right = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 4)).Value;

        // Act:
        var result = left.Overlaps(right);

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_DisjointRanges_ShouldReturnFalse()
    {
        // Arrange:
        var left = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2)).Value;
        var right = DateRange.Create(
            new DateOnly(2026, 1, 3),
            new DateOnly(2026, 1, 4)).Value;

        // Act:
        var result = left.Overlaps(right);

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_TouchingAtEndpoint_ShouldReturnTrue()
    {
        // Arrange:
        var left = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2)).Value;
        var right = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 3)).Value;

        // Act:
        var result = left.Overlaps(right);

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_OuterContainsInner_ShouldReturnTrue()
    {
        // Arrange:
        var outer = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 10)).Value;
        var inner = DateRange.Create(
            new DateOnly(2026, 1, 3),
            new DateOnly(2026, 1, 5)).Value;

        // Act:
        var result = outer.Overlaps(inner);

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_InnerOverlapsOuter_ShouldReturnTrue()
    {
        // Arrange:
        var inner = DateRange.Create(
            new DateOnly(2026, 1, 3),
            new DateOnly(2026, 1, 5)).Value;
        var outer = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 10)).Value;

        // Act:
        var result = inner.Overlaps(outer);

        // Assert:
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_DisjointWhenLeftStartsAfterRightEnds_ShouldReturnFalse()
    {
        // Arrange:
        var left = DateRange.Create(
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 1, 6)).Value;
        var right = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2)).Value;

        // Act:
        var result = left.Overlaps(right);

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_AdjacentDayAfter_ShouldReturnFalse()
    {
        // Arrange:
        var left = DateRange.Create(
            new DateOnly(2026, 1, 3),
            new DateOnly(2026, 1, 4)).Value;
        var right = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2)).Value;

        // Act:
        var result = left.Overlaps(right);

        // Assert:
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_SameSingleDay_ShouldReturnTrue()
    {
        // Arrange:
        var date = new DateOnly(2026, 1, 2);
        var left = DateRange.Create(date, date).Value;
        var right = DateRange.Create(date, date).Value;

        // Act:
        var result = left.Overlaps(right);

        // Assert:
        result.Should().BeTrue();
    }
}