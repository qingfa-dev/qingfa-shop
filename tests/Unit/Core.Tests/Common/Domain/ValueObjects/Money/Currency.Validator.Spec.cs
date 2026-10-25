using Core.Common.Domain.ValueObjects.Money;

namespace Core.Tests.Domain.ValueObjects.Money;

public class CurrencyValidatorSpec
{
    [Fact]
    public void Validate_ValidCode_ShouldSucceed()
    {
        // Arrange
        var candidate = new Currency("USD");

        // Act
        var result = CurrencyValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("USD");
    }

    [Fact]
    public void Validate_NullCode_ShouldFailWithCodeRequired()
    {
        // Arrange
        var candidate = new Currency(null);

        // Act
        var result = CurrencyValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Currency.Code.Required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_ShouldFailWithCodeRequired(string? code)
    {
        // Arrange
        var candidate = new Currency(code);

        // Act
        var result = CurrencyValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Currency.Code.Required");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("US1")]
    [InlineData("usd")]
    public void Validate_MalformedCode_ShouldFailWithCodeInvalid(string? code)
    {
        // Arrange
        var candidate = new Currency(code);

        // Act
        var result = CurrencyValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Currency.Code.Invalid");
    }
}