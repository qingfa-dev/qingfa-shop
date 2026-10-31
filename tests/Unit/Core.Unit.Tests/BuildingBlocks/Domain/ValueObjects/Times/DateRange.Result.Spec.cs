using Core.BuildingBlocks.Domain.ValueObjects.Times;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Times;

public class DateRangeResultSpec
{
    [Fact]
    public void RangeInvalid_ShouldHaveExpectedCode()
    {
        DateRangeOutcome.Failure.RangeInvalid.Code.Should().Be("DateRange.Range.Invalid");
    }

    [Fact]
    public void RangeInvalid_ShouldHaveExpectedMessage()
    {
        DateRangeOutcome.Failure.RangeInvalid.Message.Should().Be("Start date must not be after end date.");
    }

    [Fact]
    public void Create_ReversedDates_ShouldSurfaceRangeInvalid()
    {
        // Arrange & Act:
        var result = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 1));

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(DateRangeOutcome.Failure.RangeInvalid.Code);
        result.Errors![0].Message.Should().Be(DateRangeOutcome.Failure.RangeInvalid.Message);
    }

    [Fact]
    public void Validate_ReversedRange_ShouldSurfaceRangeInvalid()
    {
        // Arrange:
        var range = new DateRange(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 1));

        // Act:
        var result = DateRangeValidator.Validate(range);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(DateRangeOutcome.Failure.RangeInvalid.Code);
        result.Errors![0].Message.Should().Be(DateRangeOutcome.Failure.RangeInvalid.Message);
    }
}