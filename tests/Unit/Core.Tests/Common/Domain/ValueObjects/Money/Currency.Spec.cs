using Core.Common.Domain.ValueObjects.Money;

namespace Core.Tests.Domain.ValueObjects.Money;

public class CurrencySpec
{
    [Fact]
    public void Create_ValidCode_ShouldTrimAndUppercaseCode()
    {
        // Arrange & Act
        var result = Currency.Create(" usd ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("USD");
    }

    [Fact]
    public void Create_LowercaseCode_ShouldUppercaseCode()
    {
        // Arrange & Act
        var result = Currency.Create("usd");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("USD");
    }

    [Fact]
    public void Create_MissingCode_ShouldFailWithCodeRequired()
    {
        // Arrange & Act
        var result = Currency.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Currency.Code.Required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespace_ShouldFailWithCodeRequired(string? code)
    {
        // Arrange & Act
        var result = Currency.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Currency.Code.Required");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("US1")]
    [InlineData("12A")]
    public void Create_MalformedCode_ShouldFailWithCodeInvalid(string code)
    {
        // Arrange & Act
        var result = Currency.Create(code);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Currency.Code.Invalid");
    }
}