using Core.Common.Domain.ValueObjects.Times;

namespace Core.Tests.Common.Domain.ValueObjects.Times;

public class TimeRangeValidatorSpec
{
    [Fact]
    public void Validate_OrderedTimes_ShouldSucceed()
    {
        // Arrange
        var range = new TimeRange(
            new TimeOnly(9, 0),
            new TimeOnly(10, 0));

        // Act
        var result = TimeRangeValidator.Validate(range);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReversedTimes_ShouldFailWithInvalidRange()
    {
        // Arrange
        var range = new TimeRange(
            new TimeOnly(10, 0),
            new TimeOnly(9, 0));

        // Act
        var result = TimeRangeValidator.Validate(range);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("TimeRange.Range.Invalid");
    }
}