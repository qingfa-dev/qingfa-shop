using Core.BuildingBlocks.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Identity;

public class UrlResultSpec
{
    [Fact]
    public void ValueRequired_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = UrlOutcome.Failure.ValueRequired;

        // Assert
        error.Code.Should().Be("Url.Value.Required");
        error.Message.Should().Be("Url must be specified.");
    }

    [Fact]
    public void ValueTooLong_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = UrlOutcome.Failure.ValueTooLong;

        // Assert
        error.Code.Should().Be("Url.Value.TooLong");
        error.Message.Should().Be("Url cannot exceed 2048 characters.");
    }

    [Fact]
    public void ValueInvalid_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = UrlOutcome.Failure.ValueInvalid;

        // Assert
        error.Code.Should().Be("Url.Value.Invalid");
        error.Message.Should().Be("Url must be an absolute http(s) address.");
    }
}