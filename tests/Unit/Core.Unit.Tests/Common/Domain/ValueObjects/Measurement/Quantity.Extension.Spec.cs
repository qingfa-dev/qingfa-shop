using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Measurement;

public class QuantityExtensionSpec
{
    [Fact]
    public void Add_SameUnit_ShouldSumValues()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var left = Quantity.Create(4m, unit).Value;
        var right = Quantity.Create(6m, unit).Value;

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(10m);
        result.Value.Unit.Code.Should().Be("kg");
    }

    [Fact]
    public void Add_ZeroValue_ShouldReturnOtherOperand()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var left = Quantity.Create(0m, unit).Value;
        var right = Quantity.Create(4m, unit).Value;

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(4m);
    }

    [Fact]
    public void Add_NegativeOperands_ShouldSumWithoutValidation()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var left = new Quantity(-1m, unit);
        var right = new Quantity(4m, unit);

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(3m);
    }

    [Fact]
    public void Add_DifferentUnits_ShouldFailWithUnitMismatch()
    {
        // Arrange
        var left = Quantity.Create(4m, MeasurementUnit.Create("kg").Value).Value;
        var right = Quantity.Create(4m, MeasurementUnit.Create("g").Value).Value;

        // Act
        var result = left.Add(right);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Unit.Mismatch");
    }
}