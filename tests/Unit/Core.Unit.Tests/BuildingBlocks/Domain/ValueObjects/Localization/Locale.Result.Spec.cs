using Core.BuildingBlocks.Domain.ValueObjects.Localization;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Localization;

public class LocaleResultSpec
{
    [Fact]
    public void CodeRequired_ShouldHaveExpectedCode()
    {
        LocaleOutcome.Failure.CodeRequired.Code.Should().Be("Locale.Code.Required");
    }

    [Fact]
    public void CodeRequired_ShouldHaveExpectedMessage()
    {
        LocaleOutcome.Failure.CodeRequired.Message.Should().Be("Locale must be specified.");
    }

    [Fact]
    public void CodeTooLong_ShouldHaveExpectedCode()
    {
        LocaleOutcome.Failure.CodeTooLong.Code.Should().Be("Locale.Code.TooLong");
    }

    [Fact]
    public void CodeTooLong_ShouldHaveExpectedMessage()
    {
        LocaleOutcome.Failure.CodeTooLong.Message.Should().Be("Locale cannot exceed 35 characters.");
    }

    [Fact]
    public void CodeInvalid_ShouldHaveExpectedCode()
    {
        LocaleOutcome.Failure.CodeInvalid.Code.Should().Be("Locale.Code.Invalid");
    }

    [Fact]
    public void CodeInvalid_ShouldHaveExpectedMessage()
    {
        LocaleOutcome.Failure.CodeInvalid.Message.Should().Be(
            "Locale must be a BCP-47 tag such as 'en', 'en-US' or 'zh-Hans-CN'.");
    }

    [Fact]
    public void Create_MissingCode_ShouldSurfaceCodeRequired()
    {
        // Arrange & Act
        var result = Locale.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LocaleOutcome.Failure.CodeRequired.Code);
        result.Errors![0].Message.Should().Be(LocaleOutcome.Failure.CodeRequired.Message);
    }

    [Fact]
    public void Create_TooLongCode_ShouldSurfaceCodeTooLong()
    {
        // Arrange & Act
        var result = Locale.Create(new string('a', 36));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LocaleOutcome.Failure.CodeTooLong.Code);
        result.Errors![0].Message.Should().Be(LocaleOutcome.Failure.CodeTooLong.Message);
    }

    [Fact]
    public void Create_InvalidCode_ShouldSurfaceCodeInvalid()
    {
        // Arrange & Act
        var result = Locale.Create("en-us");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be(LocaleOutcome.Failure.CodeInvalid.Code);
        result.Errors![0].Message.Should().Be(LocaleOutcome.Failure.CodeInvalid.Message);
    }
}