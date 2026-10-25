using Core.Common.Domain.ValueObjects.Times;

namespace Core.Tests.Common.Domain.ValueObjects.Times;

public class TimeRangeResultSpec
{
    [Fact]
    public void RangeInvalid_ShouldHaveExpectedCode()
    {
        TimeRangeOutcome.Failure.RangeInvalid.Code.Should().Be("TimeRange.Range.Invalid");
    }

    [Fact]
    public void RangeInvalid_ShouldHaveExpectedMessage()
    {
        TimeRangeOutcome.Failure.RangeInvalid.Message.Should().Be("Start time must not be after end time.");
    }

    [Fact]
    public void Create_ReversedTimes_ShouldSurfaceRangeInvalid()
    {
        // Arrange & Act
        var result = TimeRange.Create(
            new TimeOnly(10, 0),
            new TimeOnly(9, 0));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(TimeRangeOutcome.Failure.RangeInvalid.Code);
        result.Errors![0].Message.Should().Be(TimeRangeOutcome.Failure.RangeInvalid.Message);
    }

    [Fact]
    public void Validate_ReversedRange_ShouldSurfaceRangeInvalid()
    {
        // Arrange
        var range = new TimeRange(
            new TimeOnly(10, 0),
            new TimeOnly(9, 0));

        // Act
        var result = TimeRangeValidator.Validate(range);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(TimeRangeOutcome.Failure.RangeInvalid.Code);
        result.Errors![0].Message.Should().Be(TimeRangeOutcome.Failure.RangeInvalid.Message);
    }
}