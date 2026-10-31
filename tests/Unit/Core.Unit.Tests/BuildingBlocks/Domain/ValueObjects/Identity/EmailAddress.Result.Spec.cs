using Core.BuildingBlocks.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Identity;

public class EmailAddressOutcomeSpec
{
    [Fact]
    public void ValueRequired_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = EmailAddressOutcome.Failure.ValueRequired;

        // Assert
        error.Code.Should().Be("EmailAddress.Value.Required");
        error.Message.Should().Be("Email address must be specified.");
    }

    [Fact]
    public void ValueTooLong_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = EmailAddressOutcome.Failure.ValueTooLong;

        // Assert
        error.Code.Should().Be("EmailAddress.Value.TooLong");
        error.Message.Should().Be("Email address cannot exceed 254 characters.");
    }

    [Fact]
    public void ValueInvalid_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = EmailAddressOutcome.Failure.ValueInvalid;

        // Assert
        error.Code.Should().Be("EmailAddress.Value.Invalid");
        error.Message.Should().Be("Email address format is invalid.");
    }
}