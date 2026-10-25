using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Tests.Common.Domain.ValueObjects.Measurement;

public class WeightValidatorSpec
{
    [Fact]
    public void Validate_ValidWeight_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var candidate = new Weight(3m, unit);

        // Act
        var result = WeightValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(candidate);
    }

    [Fact]
    public void Validate_NegativeValue_ShouldFailWithValueNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("kg").Value;
        var candidate = new Weight(-1m, unit);

        // Act
        var result = WeightValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Value.Negative");
    }

    [Fact]
    public void Validate_InvalidUnit_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var candidate = new Weight(3m, new MeasurementUnit("123"));

        // Act
        var result = WeightValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Unit.Invalid");
    }

    [Fact]
    public void Validate_NullUnitCode_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var candidate = new Weight(3m, default(MeasurementUnit));

        // Act
        var result = WeightValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Unit.Invalid");
    }

    [Fact]
    public void Validate_NegativeValueAndInvalidUnit_ShouldFailWithValueNegative()
    {
        // Arrange
        var candidate = new Weight(-1m, new MeasurementUnit("123"));

        // Act
        var result = WeightValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Weight.Value.Negative");
    }
}