using Core.BuildingBlocks.Domain.ValueObjects.Times;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Times;

public class TimeRangeExtensionSpec
{
    [Fact]
    public void Duration_NineToTen_ShouldBeOneHour()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var duration = range.Duration();

        // Assert
        duration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Duration_ZeroLengthRange_ShouldReturnZero()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(9, 0)).Value;

        // Act
        var duration = range.Duration();

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Includes_TimeInside_ShouldBeTrue()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(9, 30));

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void Includes_StartEndpoint_ShouldBeTrue()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(9, 0));

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void Includes_EndEndpoint_ShouldBeTrue()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(10, 0));

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void Includes_TimeBeforeStart_ShouldBeFalse()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(8, 59));

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void Includes_TimeAfterEnd_ShouldBeFalse()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(10, 1));

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void Includes_ZeroLengthRangeMatchingInstant_ShouldBeTrue()
    {
        // Arrange
        var time = new TimeOnly(9, 0);
        var range = TimeRange.Create(time, time).Value;

        // Act
        var included = range.Includes(time);

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void Includes_ZeroLengthRangeEarlierInstant_ShouldBeFalse()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(9, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(8, 59));

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void Includes_ZeroLengthRangeLaterInstant_ShouldBeFalse()
    {
        // Arrange
        var range = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(9, 0)).Value;

        // Act
        var included = range.Includes(new TimeOnly(9, 1));

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ShouldBeTrue()
    {
        // Arrange
        var left = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;
        var right = TimeRange.Create(
            new TimeOnly(9, 30),
            new TimeOnly(10, 30)).Value;

        // Act
        var overlapped = left.Overlaps(right);

        // Assert
        overlapped.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_DisjointRanges_ShouldBeFalse()
    {
        // Arrange
        var left = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;
        var right = TimeRange.Create(
            new TimeOnly(10, 30),
            new TimeOnly(11, 30)).Value;

        // Act
        var overlapped = left.Overlaps(right);

        // Assert
        overlapped.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_TouchingAtEndpoint_ShouldBeTrue()
    {
        // Arrange
        var left = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;
        var right = TimeRange.Create(
            new TimeOnly(10, 0),
            new TimeOnly(11, 0)).Value;

        // Act
        var overlapped = left.Overlaps(right);

        // Assert
        overlapped.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_OuterContainsInner_ShouldBeTrue()
    {
        // Arrange
        var outer = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(12, 0)).Value;
        var inner = TimeRange.Create(
            new TimeOnly(10, 0),
            new TimeOnly(11, 0)).Value;

        // Act
        var overlapped = outer.Overlaps(inner);

        // Assert
        overlapped.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_InnerOverlapsOuter_ShouldBeTrue()
    {
        // Arrange
        var inner = TimeRange.Create(
            new TimeOnly(10, 0),
            new TimeOnly(11, 0)).Value;
        var outer = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(12, 0)).Value;

        // Act
        var overlapped = inner.Overlaps(outer);

        // Assert
        overlapped.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_DisjointWhenLeftStartsAfterRightEnds_ShouldBeFalse()
    {
        // Arrange
        var left = TimeRange.Create(
            new TimeOnly(11, 0),
            new TimeOnly(12, 0)).Value;
        var right = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var overlapped = left.Overlaps(right);

        // Assert
        overlapped.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_AdjacentMinuteAfter_ShouldBeFalse()
    {
        // Arrange
        var left = TimeRange.Create(
            new TimeOnly(10, 1),
            new TimeOnly(11, 0)).Value;
        var right = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0)).Value;

        // Act
        var overlapped = left.Overlaps(right);

        // Assert
        overlapped.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_SameInstant_ShouldBeTrue()
    {
        // Arrange
        var time = new TimeOnly(9, 0);
        var left = TimeRange.Create(time, time).Value;
        var right = TimeRange.Create(time, time).Value;

        // Act
        var overlapped = left.Overlaps(right);

        // Assert
        overlapped.Should().BeTrue();
    }
}