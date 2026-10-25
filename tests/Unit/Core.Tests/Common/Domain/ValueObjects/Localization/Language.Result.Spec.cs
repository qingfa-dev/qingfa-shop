using Core.Common.Domain.ValueObjects.Localization;

namespace Core.Tests.Common.Domain.ValueObjects.Localization;

public class LanguageResultSpec
{
    [Fact]
    public void CodeRequired_ShouldHaveExpectedCode()
    {
        LanguageOutcome.Failure.CodeRequired.Code.Should().Be("Language.Code.Required");
    }

    [Fact]
    public void CodeRequired_ShouldHaveExpectedMessage()
    {
        LanguageOutcome.Failure.CodeRequired.Message.Should().Be("Language must be specified.");
    }

    [Fact]
    public void CodeTooLong_ShouldHaveExpectedCode()
    {
        LanguageOutcome.Failure.CodeTooLong.Code.Should().Be("Language.Code.TooLong");
    }

    [Fact]
    public void CodeTooLong_ShouldHaveExpectedMessage()
    {
        LanguageOutcome.Failure.CodeTooLong.Message.Should().Be("Language cannot exceed 8 characters.");
    }

    [Fact]
    public void CodeInvalid_ShouldHaveExpectedCode()
    {
        LanguageOutcome.Failure.CodeInvalid.Code.Should().Be("Language.Code.Invalid");
    }

    [Fact]
    public void CodeInvalid_ShouldHaveExpectedMessage()
    {
        LanguageOutcome.Failure.CodeInvalid.Message.Should().Be(
            "Language must be a lowercase ISO-639 subtag such as 'en' or 'fra'.");
    }

    [Fact]
    public void Create_MissingCode_ShouldSurfaceCodeRequired()
    {
        // Arrange & Act
        var result = Language.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LanguageOutcome.Failure.CodeRequired.Code);
        result.Errors![0].Message.Should().Be(LanguageOutcome.Failure.CodeRequired.Message);
    }

    [Fact]
    public void Create_TooLongCode_ShouldSurfaceCodeTooLong()
    {
        // Arrange & Act
        var result = Language.Create("abcdefghi");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LanguageOutcome.Failure.CodeTooLong.Code);
        result.Errors![0].Message.Should().Be(LanguageOutcome.Failure.CodeTooLong.Message);
    }

    [Fact]
    public void Create_InvalidCode_ShouldSurfaceCodeInvalid()
    {
        // Arrange & Act
        var result = Language.Create("abcd");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LanguageOutcome.Failure.CodeInvalid.Code);
        result.Errors![0].Message.Should().Be(LanguageOutcome.Failure.CodeInvalid.Message);
    }
}