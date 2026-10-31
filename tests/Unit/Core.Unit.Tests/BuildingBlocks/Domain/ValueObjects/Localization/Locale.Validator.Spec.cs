using Core.BuildingBlocks.Domain.ValueObjects.Localization;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Localization;

public class LocaleValidatorSpec
{
    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hans-CN")]
    [InlineData("en-123")]
    public void Validate_ValidCode_ShouldSucceed(string code)
    {
        // Arrange
        var candidate = new Locale(code);

        // Act
        var result = LocaleValidator.Validate(candidate);

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
        var candidate = new Locale(code);

        // Act
        var result = LocaleValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.Required");
    }

    [Fact]
    public void Validate_OverThirtyFiveChars_ShouldFailWithCodeTooLong()
    {
        // Arrange
        var candidate = new Locale(new string('a', 36));

        // Act
        var result = LocaleValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.TooLong");
    }

    [Theory]
    [InlineData("en-us")]
    [InlineData("EN-US")]
    [InlineData("EN")]
    [InlineData("abcd")]
    public void Validate_InvalidCode_ShouldFailWithCodeInvalid(string code)
    {
        // Arrange
        var candidate = new Locale(code);

        // Act
        var result = LocaleValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.Invalid");
    }

    [Fact]
    public void Validate_PaddedCode_ShouldFailWithCodeInvalidWithoutTrimming()
    {
        // Arrange
        var candidate = new Locale(" en-US ");

        // Act
        var result = LocaleValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.Invalid");
    }
}