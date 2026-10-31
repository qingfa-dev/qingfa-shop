using Core.BuildingBlocks.Domain.ValueObjects.Measurement;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Measurement;

public class QuantityResultSpec
{
    [Fact]
    public void Failure_ValueNegative_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        QuantityOutcome.Failure.ValueNegative.Code.Should().Be("Quantity.Value.Negative");
    }

    [Fact]
    public void Failure_UnitInvalid_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        QuantityOutcome.Failure.UnitInvalid.Code.Should().Be("Quantity.Unit.Invalid");
    }

    [Fact]
    public void Failure_UnitMismatch_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        QuantityOutcome.Failure.UnitMismatch.Code.Should().Be("Quantity.Unit.Mismatch");
    }
}