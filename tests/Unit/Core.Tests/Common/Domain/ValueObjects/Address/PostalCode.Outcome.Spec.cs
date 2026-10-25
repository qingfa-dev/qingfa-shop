using Core.Common.Domain.ValueObjects.Address;

namespace Core.Tests.Domain.ValueObjects.Address;

public class PostalCodeOutcomeSpec
{
    [Fact]
    public void ValueRequired_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = PostalCodeOutcome.Failure.ValueRequired;

        // Assert:
        error.Code.Should().Be("PostalCode.Value.Required");
    }

    [Fact]
    public void ValueTooLong_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = PostalCodeOutcome.Failure.ValueTooLong;

        // Assert:
        error.Code.Should().Be("PostalCode.Value.TooLong");
    }

    [Fact]
    public void ValueInvalid_ShouldHaveExpectedCode()
    {
        // Arrange & Act:
        var error = PostalCodeOutcome.Failure.ValueInvalid;

        // Assert:
        error.Code.Should().Be("PostalCode.Value.Invalid");
    }
}