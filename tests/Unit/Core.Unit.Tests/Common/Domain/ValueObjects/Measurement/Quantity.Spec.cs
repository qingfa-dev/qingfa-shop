using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Measurement;

public class QuantitySpec
{
    [Fact]
    public void Create_NonNegativeValue_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;

        // Act
        var result = Quantity.Create(4m, unit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(4m);
    }

    [Fact]
    public void Create_ZeroValue_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;

        // Act
        var result = Quantity.Create(0m, unit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_NegativeValue_ShouldFailWithValueNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;

        // Act
        var result = Quantity.Create(-1m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Value.Negative");
    }

    [Fact]
    public void Create_InvalidUnit_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var unit = new MeasurementUnit("123");

        // Act
        var result = Quantity.Create(4m, unit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Unit.Invalid");
    }

    [Fact]
    public void RecordEquality_ShouldCompareValueAndUnit()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var quantity = Quantity.Create(4m, unit).Value;
        var equivalent = Quantity.Create(4m, unit).Value;
        var changed = Quantity.Create(5m, unit).Value;

        // Act & Assert
        quantity.Should().Be(equivalent);
        quantity.Should().NotBe(changed);
    }
}