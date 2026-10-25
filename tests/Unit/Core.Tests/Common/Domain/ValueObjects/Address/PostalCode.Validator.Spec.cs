using Core.Common.Domain.ValueObjects.Address;

namespace Core.Tests.Common.Domain.ValueObjects.Address;

public class PostalCodeValidatorSpec
{
    [Fact]
    public void Validate_ValidCode_ShouldSucceedWithoutMutation()
    {
        // Arrange:
        var postalCode = new PostalCode("M5V 2T6");

        // Act:
        var result = PostalCodeValidator.Validate(postalCode);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postalCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange:
        var postalCode = new PostalCode(value);

        // Act:
        var result = PostalCodeValidator.Validate(postalCode);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.Required");
    }

    [Fact]
    public void Validate_ValueTooLong_ShouldFailWithValueTooLong()
    {
        // Arrange:
        var postalCode = new PostalCode(new string('A', PostalCodeConstant.Constraints.Code.MaxLength + 1));

        // Act:
        var result = PostalCodeValidator.Validate(postalCode);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.TooLong");
    }

    [Fact]
    public void Validate_ExactMaxLength_ShouldSucceed()
    {
        // Arrange:
        var postalCode = new PostalCode("A1B2C3D4E5");

        // Act:
        var result = PostalCodeValidator.Validate(postalCode);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("!")]
    [InlineData("!ABC")]
    public void Validate_InvalidCharset_ShouldFailWithValueInvalid(string? value)
    {
        // Arrange:
        var postalCode = new PostalCode(value);

        // Act:
        var result = PostalCodeValidator.Validate(postalCode);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PostalCode.Value.Invalid");
    }
}