using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Identity;

public class EmailAddressSpec
{
    [Fact]
    public void Create_ValidInput_ShouldTrimAndLowercaseValue()
    {
        // Arrange & Act
        var result = EmailAddress.Create(" Ada@example.com ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ada@example.com");
    }

    [Fact]
    public void Create_UppercaseWithWhitespace_ShouldNormalizeToLowercase()
    {
        // Arrange & Act
        var result = EmailAddress.Create("  A@B.COM  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("a@b.com");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.co")]
    [InlineData("a@b.co")]
    public void Create_ValidAddress_ShouldSucceed(string value)
    {
        // Arrange & Act
        var result = EmailAddress.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value.ToLowerInvariant());
    }

    [Theory]
    [InlineData("missing-at.com")]
    [InlineData("user@example")]
    [InlineData("user@example.c")]
    [InlineData("user@.com")]
    public void Create_InvalidPattern_ShouldFailWithValueInvalid(string value)
    {
        // Arrange & Act
        var result = EmailAddress.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.Invalid");
    }

    [Fact]
    public void Create_OverLength_ShouldFailWithValueTooLong()
    {
        // Arrange
        var value = new string('a', 243) + "@example.com";

        // Act
        var result = EmailAddress.Create(value);

        // Assert
        value.Length.Should().Be(255);
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.TooLong");
    }

    [Fact]
    public void Create_MissingValue_ShouldFailWithValueRequired()
    {
        // Arrange & Act
        var result = EmailAddress.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange & Act
        var result = EmailAddress.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("EmailAddress.Value.Required");
    }
}