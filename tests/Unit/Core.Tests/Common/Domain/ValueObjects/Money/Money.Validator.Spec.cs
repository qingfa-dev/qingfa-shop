using Core.Common.Domain.ValueObjects.Money;

namespace Core.Tests.Common.Domain.ValueObjects.Money;

public class MoneyValidatorSpec
{
    [Fact]
    public void Validate_ValidMoney_ShouldSucceed()
    {
        // Arrange
        var currency = Currency.Create("USD").Value;
        var candidate = new global::Core.Common.Domain.ValueObjects.Money.Money(12.5m, currency);

        // Act
        var result = MoneyValidator.Validate(candidate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(12.5m);
        result.Value.Currency.Should().Be(currency);
    }

    [Fact]
    public void Validate_DefaultCurrency_ShouldFailWithCurrencyInvalid()
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Money.Money(12.5m, default);

        // Act
        var result = MoneyValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Money.Currency.Invalid");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("usd")]
    [InlineData("US1")]
    public void Validate_MalformedCurrencyCode_ShouldFailWithCurrencyInvalid(string code)
    {
        // Arrange
        var candidate = new global::Core.Common.Domain.ValueObjects.Money.Money(12.5m, new Currency(code));

        // Act
        var result = MoneyValidator.Validate(candidate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Money.Currency.Invalid");
    }
}