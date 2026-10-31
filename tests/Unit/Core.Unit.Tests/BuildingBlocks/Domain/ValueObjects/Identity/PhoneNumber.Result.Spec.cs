using Core.BuildingBlocks.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Identity;

public class PhoneNumberResultSpec
{
    [Fact]
    public void ValueRequired_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = PhoneNumberOutcome.Failure.ValueRequired;

        // Assert
        error.Code.Should().Be("PhoneNumber.Value.Required");
        error.Message.Should().Be("Phone number must be specified.");
    }

    [Fact]
    public void ValueTooLong_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = PhoneNumberOutcome.Failure.ValueTooLong;

        // Assert
        error.Code.Should().Be("PhoneNumber.Value.TooLong");
        error.Message.Should().Be("Phone number cannot exceed 20 characters.");
    }

    [Fact]
    public void ValueInvalid_ShouldExposeCodeAndMessage()
    {
        // Arrange & Act
        var error = PhoneNumberOutcome.Failure.ValueInvalid;

        // Assert
        error.Code.Should().Be("PhoneNumber.Value.Invalid");
        error.Message.Should().Be("Phone number format is invalid.");
    }
}