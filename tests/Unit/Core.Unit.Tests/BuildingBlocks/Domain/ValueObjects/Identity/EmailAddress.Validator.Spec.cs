using Core.BuildingBlocks.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Identity;

public class EmailAddressValidatorSpec
{
    [Fact]
    public void Validate_ValidCandidate_ShouldSucceed()
    {
        // Arrange
        var candidate = new EmailAddress("ada@example.com");

        // Act
        var result = EmailAddressValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ada@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange
        var candidate = new EmailAddress(value);

        // Act
        var result = EmailAddressValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.Required");
    }

    [Fact]
    public void Validate_OverLength_ShouldFailWithValueTooLong()
    {
        // Arrange
        var candidate = new EmailAddress(new string('a', 243) + "@example.com");

        // Act
        var result = EmailAddressValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.TooLong");
    }

    [Theory]
    [InlineData("missing-at.com")]
    [InlineData("user@example")]
    [InlineData("user@example.c")]
    public void Validate_InvalidPattern_ShouldFailWithValueInvalid(string value)
    {
        // Arrange
        var candidate = new EmailAddress(value);

        // Act
        var result = EmailAddressValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.Invalid");
    }
}