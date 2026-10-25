using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Tests.Domain.ValueObjects.Measurement;

public class DimensionsValidatorSpec
{
    [Fact]
    public void Validate_ValidDimensions_ShouldSucceed()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;
        var candidate = new Dimensions(1m, 2m, 3m, unit);

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(candidate);
    }

    [Fact]
    public void Validate_NegativeLength_ShouldFailWithLengthNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;
        var candidate = new Dimensions(-1m, 2m, 3m, unit);

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Length.Negative");
    }

    [Fact]
    public void Validate_NegativeWidth_ShouldFailWithWidthNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;
        var candidate = new Dimensions(1m, -2m, 3m, unit);

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Width.Negative");
    }

    [Fact]
    public void Validate_NegativeHeight_ShouldFailWithHeightNegative()
    {
        // Arrange
        var unit = MeasurementUnit.Create("cm").Value;
        var candidate = new Dimensions(1m, 2m, -3m, unit);

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Height.Negative");
    }

    [Fact]
    public void Validate_InvalidUnit_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var candidate = new Dimensions(1m, 2m, 3m, new MeasurementUnit("123"));

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Unit.Invalid");
    }

    [Fact]
    public void Validate_NullUnitCode_ShouldFailWithUnitInvalid()
    {
        // Arrange
        var candidate = new Dimensions(1m, 2m, 3m, default(MeasurementUnit));

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Unit.Invalid");
    }

    [Fact]
    public void Validate_NegativeValueAndInvalidUnit_ShouldFailWithFirstError()
    {
        // Arrange
        var candidate = new Dimensions(-1m, 2m, 3m, new MeasurementUnit("123"));

        // Act
        var result = DimensionsValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Dimensions.Length.Negative");
    }
}