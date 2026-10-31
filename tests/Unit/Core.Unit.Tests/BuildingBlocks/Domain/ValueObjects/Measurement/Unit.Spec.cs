using MeasurementUnit = Core.BuildingBlocks.Domain.ValueObjects.Measurement.Unit;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Measurement;

public class UnitSpec
{
    [Fact]
    public void Create_ValidCode_ShouldTrimAndLowercaseCode()
    {
        // Arrange & Act
        var result = MeasurementUnit.Create(" KG ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("kg");
    }

    [Fact]
    public void Create_UppercaseCode_ShouldNormalizeToLowercase()
    {
        // Arrange & Act
        var result = MeasurementUnit.Create("KG");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("kg");
    }

    [Fact]
    public void Create_MissingCode_ShouldFailWithCodeRequired()
    {
        // Arrange & Act
        var result = MeasurementUnit.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.Required");
    }

    [Fact]
    public void Create_EmptyCode_ShouldFailWithCodeRequired()
    {
        // Arrange & Act
        var result = MeasurementUnit.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.Required");
    }

    [Fact]
    public void Create_TooLongCode_ShouldFailWithCodeTooLong()
    {
        // Arrange & Act
        var result = MeasurementUnit.Create("abcde");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.TooLong");
    }

    [Fact]
    public void Create_CodeWithDigits_ShouldFailWithCodeInvalid()
    {
        // Arrange & Act
        var result = MeasurementUnit.Create("k1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Unit.Code.Invalid");
    }
}