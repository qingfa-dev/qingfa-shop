using Core.Common.Domain.ValueObjects.Measurement;

using MeasurementUnit = Core.Common.Domain.ValueObjects.Measurement.Unit;

namespace Core.Tests.Domain.ValueObjects.Measurement;

public class UnitValidatorSpec
{
    [Fact]
    public void Validate_ValidCode_ShouldSucceed()
    {
        // Arrange
        var candidate = new MeasurementUnit("kg");

        // Act
        var result = UnitValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(candidate);
    }

    [Fact]
    public void Validate_MissingCode_ShouldFailWithCodeRequired()
    {
        // Arrange
        var candidate = new MeasurementUnit(null);

        // Act
        var result = UnitValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.Required");
    }

    [Fact]
    public void Validate_TooLongCode_ShouldFailWithCodeTooLong()
    {
        // Arrange
        var candidate = new MeasurementUnit("abcde");

        // Act
        var result = UnitValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.TooLong");
    }

    [Fact]
    public void Validate_CodeWithDigits_ShouldFailWithCodeInvalid()
    {
        // Arrange
        var candidate = new MeasurementUnit("k1");

        // Act
        var result = UnitValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.Invalid");
    }
}