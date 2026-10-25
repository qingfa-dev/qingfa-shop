using Core.Common.Domain.ValueObjects.Localization;

namespace Core.Tests.Domain.ValueObjects.Localization;

public class LocaleResultSpec
{
    [Fact]
    public void CodeRequired_ShouldHaveExpectedCode()
    {
        LocaleResult.Failure.CodeRequired.Code.Should().Be("Locale.Code.Required");
    }

    [Fact]
    public void CodeRequired_ShouldHaveExpectedMessage()
    {
        LocaleResult.Failure.CodeRequired.Message.Should().Be("Locale must be specified.");
    }

    [Fact]
    public void CodeTooLong_ShouldHaveExpectedCode()
    {
        LocaleResult.Failure.CodeTooLong.Code.Should().Be("Locale.Code.TooLong");
    }

    [Fact]
    public void CodeTooLong_ShouldHaveExpectedMessage()
    {
        LocaleResult.Failure.CodeTooLong.Message.Should().Be("Locale cannot exceed 35 characters.");
    }

    [Fact]
    public void CodeInvalid_ShouldHaveExpectedCode()
    {
        LocaleResult.Failure.CodeInvalid.Code.Should().Be("Locale.Code.Invalid");
    }

    [Fact]
    public void CodeInvalid_ShouldHaveExpectedMessage()
    {
        LocaleResult.Failure.CodeInvalid.Message.Should().Be(
            "Locale must be a BCP-47 tag such as 'en', 'en-US' or 'zh-Hans-CN'.");
    }

    [Fact]
    public void Create_MissingCode_ShouldSurfaceCodeRequired()
    {
        // Arrange & Act
        var result = Locale.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LocaleResult.Failure.CodeRequired.Code);
        result.Errors![0].Message.Should().Be(LocaleResult.Failure.CodeRequired.Message);
    }

    [Fact]
    public void Create_TooLongCode_ShouldSurfaceCodeTooLong()
    {
        // Arrange & Act
        var result = Locale.Create(new string('a', 36));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LocaleResult.Failure.CodeTooLong.Code);
        result.Errors![0].Message.Should().Be(LocaleResult.Failure.CodeTooLong.Message);
    }

    [Fact]
    public void Create_InvalidCode_ShouldSurfaceCodeInvalid()
    {
        // Arrange & Act
        var result = Locale.Create("en-us");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LocaleResult.Failure.CodeInvalid.Code);
        result.Errors![0].Message.Should().Be(LocaleResult.Failure.CodeInvalid.Message);
    }
}