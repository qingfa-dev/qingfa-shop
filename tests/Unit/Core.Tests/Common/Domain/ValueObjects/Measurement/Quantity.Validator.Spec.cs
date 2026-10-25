using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Tests.Domain.ValueObjects.Measurement;

public class QuantityValidatorSpec
{
    [Fact]
    public void Validate_ValidQuantity_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var candidate = new Quantity(4m, unit);

        // Act
        var result = QuantityValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(candidate);
    }

    [Fact]
    public void Validate_NegativeValue_ShouldFailWithValueNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var candidate = new Quantity(-1m, unit);

        // Act
        var result = QuantityValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Value.Negative");
    }

    [Fact]
    public void Validate_InvalidUnit_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var candidate = new Quantity(4m, new MeasurementUnit("123"));

        // Act
        var result = QuantityValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Unit.Invalid");
    }

    [Fact]
    public void Validate_NullUnitCode_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var candidate = new Quantity(4m, default(MeasurementUnit));

        // Act
        var result = QuantityValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Unit.Invalid");
    }

    [Fact]
    public void Validate_NegativeValueAndInvalidUnit_ShouldFailWithValueNegative()
    {
        // Arrange
        var candidate = new Quantity(-1m, new MeasurementUnit("123"));

        // Act
        var result = QuantityValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Quantity.Value.Negative");
    }
}