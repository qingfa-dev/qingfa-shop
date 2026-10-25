using Core.Common.Domain.ValueObjects.Localization;

namespace Core.Tests.Common.Domain.ValueObjects.Localization;

public class LocaleSpec
{
    [Fact]
    public void Create_ValidInput_ShouldTrimCode()
    {
        // Arrange & Act
        var result = Locale.Create(" en-US ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("en-US");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fra")]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hans-CN")]
    [InlineData("en-123")]
    public void Create_ValidSubtags_ShouldSucceed(string code)
    {
        // Arrange & Act
        var result = Locale.Create(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code);
    }

    [Fact]
    public void Create_MissingCode_ShouldFailWithCodeRequired()
    {
        // Arrange & Act
        var result = Locale.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.Required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespace_ShouldFailWithCodeRequired(string code)
    {
        // Arrange & Act
        var result = Locale.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.Required");
    }

    [Fact]
    public void Create_OverThirtyFiveChars_ShouldFailWithCodeTooLong()
    {
        // Arrange
        var code = new string('a', 36);

        // Act
        var result = Locale.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.TooLong");
    }

    [Theory]
    [InlineData("en-us")]
    [InlineData("EN")]
    [InlineData("EN-US")]
    [InlineData("abcd")]
    [InlineData("e1")]
    public void Create_InvalidCode_ShouldFailWithCodeInvalid(string code)
    {
        // Arrange & Act
        var result = Locale.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Locale.Code.Invalid");
    }
}