using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Unit.Tests.Common.Domain.ValueObjects.Identity;

public class PhoneNumberSpec
{
    [Fact]
    public void Create_ValidInput_ShouldTrimValue()
    {
        // Arrange & Act
        var result = PhoneNumber.Create(" +1 (555) 123-4567 ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+1 (555) 123-4567");
    }

    [Theory]
    [InlineData("+1 (555) 123-4567")]
    [InlineData("(555) 123 4567")]
    [InlineData("555-1234")]
    [InlineData("+442071234567")]
    public void Create_ValidPattern_ShouldSucceed(string value)
    {
        // Arrange & Act
        var result = PhoneNumber.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_Whitespace_ShouldTrimValue()
    {
        // Arrange & Act
        var result = PhoneNumber.Create("  555-1234  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("555-1234");
    }

    [Fact]
    public void Create_MissingValue_ShouldFailWithValueRequired()
    {
        // Arrange & Act
        var result = PhoneNumber.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyValue_ShouldFailWithValueRequired(string? value)
    {
        // Arrange & Act
        var result = PhoneNumber.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.Required");
    }

    [Fact]
    public void Create_TooShort_ShouldFailWithValueInvalid()
    {
        // Arrange & Act
        var result = PhoneNumber.Create("1234");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.Invalid");
    }

    [Fact]
    public void Create_OverLimit_ShouldFailWithValueTooLong()
    {
        // Arrange
        var value = new string('1', 21);

        // Act
        var result = PhoneNumber.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.TooLong");
    }

    [Theory]
    [InlineData("ABC123")]
    [InlineData("555-ABCD")]
    public void Create_Letters_ShouldFailWithValueInvalid(string value)
    {
        // Arrange & Act
        var result = PhoneNumber.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("PhoneNumber.Value.Invalid");
    }
}