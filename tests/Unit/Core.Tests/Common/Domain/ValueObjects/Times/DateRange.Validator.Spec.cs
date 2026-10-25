using Core.Common.Domain.ValueObjects.Times;

namespace Core.Tests.Domain.ValueObjects.Times;

public class DateRangeValidatorSpec
{
    [Fact]
    public void Validate_OrderedRange_ShouldSucceed()
    {
        // Arrange:
        var range = new DateRange(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2));

        // Act:
        var result = DateRangeValidator.Validate(range);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(range);
    }

    [Fact]
    public void Validate_ReversedRange_ShouldFailWithInvalidRange()
    {
        // Arrange:
        var range = new DateRange(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 1));

        // Act:
        var result = DateRangeValidator.Validate(range);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("DateRange.Range.Invalid");
    }
}