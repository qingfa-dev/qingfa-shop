using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Identity;

public class PhoneNumberValidatorSpec
{
    [Fact]
    public void Validate_ValidCandidate_ShouldSucceed()
    {
        // Arrange
        var candidate = new PhoneNumber("+1 (555) 123-4567");

        // Act
        var result = PhoneNumberValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+1 (555) 123-4567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange
        var candidate = new PhoneNumber(value);

        // Act
        var result = PhoneNumberValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.Required");
    }

    [Fact]
    public void Validate_OverLimit_ShouldFailWithValueTooLong()
    {
        // Arrange
        var candidate = new PhoneNumber(new string('1', 21));

        // Act
        var result = PhoneNumberValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.TooLong");
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("ABC123")]
    public void Validate_InvalidPattern_ShouldFailWithValueInvalid(string value)
    {
        // Arrange
        var candidate = new PhoneNumber(value);

        // Act
        var result = PhoneNumberValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.Invalid");
    }
}