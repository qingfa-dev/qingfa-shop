using Core.Common.Domain.ValueObjects.Times;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Times;

public class TimeRangeSpec
{
    [Fact]
    public void Create_OrderedTimes_ShouldSucceed()
    {
        var result = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_EndBeforeStart_ShouldFailWithInvalidRange()
    {
        var result = TimeRange.Create(
            new TimeOnly(10, 0),
            new TimeOnly(9, 0));

        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("TimeRange.Range.Invalid");
    }

    [Fact]
    public void Create_StartEqualsEnd_ShouldSucceedWithZeroDuration()
    {
        // Arrange & Act
        var result = TimeRange.Create(
            new TimeOnly(9, 0),
            new TimeOnly(9, 0));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Duration().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RecordEquality_SameTimes_ShouldBeEqual()
    {
        var range = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(10, 0)).Value;
        var equivalent = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(10, 0)).Value;
        var changed = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(11, 0)).Value;

        range.Should().Be(equivalent);
        range.Should().NotBe(changed);
    }
}