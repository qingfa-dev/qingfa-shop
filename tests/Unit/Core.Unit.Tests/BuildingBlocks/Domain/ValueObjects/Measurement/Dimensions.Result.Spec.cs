using Core.BuildingBlocks.Domain.ValueObjects.Measurement;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Measurement;

public class DimensionsResultSpec
{
    [Fact]
    public void Failure_LengthNegative_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        DimensionsOutcome.Failure.LengthNegative.Code.Should().Be("Dimensions.Length.Negative");
    }

    [Fact]
    public void Failure_WidthNegative_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        DimensionsOutcome.Failure.WidthNegative.Code.Should().Be("Dimensions.Width.Negative");
    }

    [Fact]
    public void Failure_HeightNegative_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        DimensionsOutcome.Failure.HeightNegative.Code.Should().Be("Dimensions.Height.Negative");
    }

    [Fact]
    public void Failure_UnitInvalid_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        DimensionsOutcome.Failure.UnitInvalid.Code.Should().Be("Dimensions.Unit.Invalid");
    }
}