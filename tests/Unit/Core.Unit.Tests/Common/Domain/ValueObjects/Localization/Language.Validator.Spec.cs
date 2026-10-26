using Core.Common.Domain.ValueObjects.Localization;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Localization;

public class LanguageValidatorSpec
{
    [Theory]
    [InlineData("en")]
    [InlineData("fra")]
    public void Validate_ValidCode_ShouldSucceed(string code)
    {
        // Arrange
        var candidate = new Language(code);

        // Act
        var result = LanguageValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingCode_ShouldFailWithCodeRequired(string? code)
    {
        // Arrange
        var candidate = new Language(code);

        // Act
        var result = LanguageValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Required");
    }

    [Fact]
    public void Validate_NineCharCode_ShouldFailWithCodeTooLong()
    {
        // Arrange
        var candidate = new Language("abcdefghi");

        // Act
        var result = LanguageValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.TooLong");
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("EN")]
    [InlineData("e1")]
    [InlineData("e")]
    public void Validate_InvalidCode_ShouldFailWithCodeInvalid(string code)
    {
        // Arrange
        var candidate = new Language(code);

        // Act
        var result = LanguageValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Invalid");
    }

    [Fact]
    public void Validate_PaddedCode_ShouldFailWithCodeInvalidWithoutTrimming()
    {
        // Arrange
        var candidate = new Language(" en ");

        // Act
        var result = LanguageValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Invalid");
    }
}