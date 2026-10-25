using Core.Common.Domain.ValueObjects.Localization;

namespace Core.Tests.Common.Domain.ValueObjects.Localization;

public class LanguageSpec
{
    [Fact]
    public void Create_ValidInput_ShouldTrimAndNormalizeCode()
    {
        // Arrange & Act
        var result = Language.Create(" en ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("en");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("fra")]
    [InlineData("deu")]
    public void Create_TwoOrThreeLetterCode_ShouldSucceed(string code)
    {
        // Arrange & Act
        var result = Language.Create(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code);
    }

    [Theory]
    [InlineData(" en ")]
    [InlineData("\ten\n")]
    public void Create_PaddedCode_ShouldTrim(string code)
    {
        // Arrange & Act
        var result = Language.Create(code);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code.Trim());
    }

    [Fact]
    public void Create_MissingCode_ShouldFailWithCodeRequired()
    {
        // Arrange & Act
        var result = Language.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespace_ShouldFailWithCodeRequired(string code)
    {
        // Arrange & Act
        var result = Language.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Required");
    }

    [Fact]
    public void Create_FourLetterCode_ShouldFailWithCodeInvalidNotTooLong()
    {
        // Arrange & Act
        var result = Language.Create("abcd");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Invalid");
    }

    [Fact]
    public void Create_NineCharCode_ShouldFailWithCodeTooLong()
    {
        // Arrange & Act
        var result = Language.Create("abcdefghi");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.TooLong");
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("En")]
    public void Create_UppercaseCode_ShouldFailWithCodeInvalid(string code)
    {
        // Arrange & Act
        var result = Language.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Invalid");
    }

    [Theory]
    [InlineData("e1")]
    [InlineData("12")]
    [InlineData("en1")]
    public void Create_DigitCode_ShouldFailWithCodeInvalid(string code)
    {
        // Arrange & Act
        var result = Language.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Language.Code.Invalid");
    }
}