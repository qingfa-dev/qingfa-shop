using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Measurement;

public class WeightExtensionSpec
{
    [Fact]
    public void Add_SameUnit_ShouldSumValues()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var left = Weight.Create(3m, unit).Value;
        var right = Weight.Create(2m, unit).Value;

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(5m);
        result.Value.Unit.Code.Should().Be("kg");
    }

    [Fact]
    public void Add_ZeroValue_ShouldReturnOtherOperand()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var left = Weight.Create(0m, unit).Value;
        var right = Weight.Create(3m, unit).Value;

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(3m);
    }

    [Fact]
    public void Add_NegativeOperands_ShouldSumWithoutValidation()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var left = new Weight(-1m, unit);
        var right = new Weight(3m, unit);

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(2m);
    }

    [Fact]
    public void Add_DifferentUnits_ShouldFailWithUnitMismatch()
    {
        // Arrange
        var left = Weight.Create(3m, MeasurementUnit.Create("kg").Value).Value;
        var right = Weight.Create(3m, MeasurementUnit.Create("g").Value).Value;

        // Act
        var result = left.Add(right);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Unit.Mismatch");
    }
}