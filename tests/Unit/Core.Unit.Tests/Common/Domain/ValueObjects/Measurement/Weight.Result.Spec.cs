using Core.Common.Domain.ValueObjects.Measurement;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Measurement;

public class WeightResultSpec
{
    [Fact]
    public void Failure_ValueNegative_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        WeightOutcome.Failure.ValueNegative.Code.Should().Be("Weight.Value.Negative");
    }

    [Fact]
    public void Failure_UnitInvalid_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        WeightOutcome.Failure.UnitInvalid.Code.Should().Be("Weight.Unit.Invalid");
    }

    [Fact]
    public void Failure_UnitMismatch_ShouldHaveExpectedCode()
    {
        // Arrange, Act & Assert
        WeightOutcome.Failure.UnitMismatch.Code.Should().Be("Weight.Unit.Mismatch");
    }
}