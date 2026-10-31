using Core.BuildingBlocks.Domain.ValueObjects.Percentage;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Percentage;

public class PercentageResultSpec
{
    [Fact]
    public void ValueOutOfRange_ShouldHaveExpectedCode()
    {
        PercentageOutcome.Failure.ValueOutOfRange.Code.Should().Be("Percentage.Value.OutOfRange");
    }

    [Fact]
    public void ValueOutOfRange_ShouldHaveExpectedMessage()
    {
        PercentageOutcome.Failure.ValueOutOfRange.Message.Should().Be("Percentage must be between 0 and 100.");
    }

    [Fact]
    public void Create_OutOfRange_ShouldSurfaceValueOutOfRange()
    {
        // Arrange & Act
        var result = global::Core.BuildingBlocks.Domain.ValueObjects.Percentage.Percentage.Create(101m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(PercentageOutcome.Failure.ValueOutOfRange.Code);
        result.Errors![0].Message.Should().Be(PercentageOutcome.Failure.ValueOutOfRange.Message);
    }
}