using Core.Common.Domain.ValueObjects.Money;

namespace Core.Tests.Common.Domain.ValueObjects.Money;

public class MoneySpec
{
    [Fact]
    public void Create_ValidCurrency_ShouldSucceed()
    {
        // Arrange
        var currency = Currency.Create("USD").Value;

        // Act
        var result = global::Core.Common.Domain.ValueObjects.Money.Money.Create(12.5m, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(12.5m);
        result.Value.Currency.Should().Be(currency);
    }

    [Fact]
    public void Create_InvalidCurrency_ShouldFailWithCurrencyInvalid()
    {
        // Arrange & Act
        var result = global::Core.Common.Domain.ValueObjects.Money.Money.Create(12.5m, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Money.Currency.Invalid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12.5)]
    public void Create_ZeroOrNegativeAmount_WithValidCurrency_ShouldSucceed(decimal amount)
    {
        // Arrange
        var currency = Currency.Create("USD").Value;

        // Act
        var result = global::Core.Common.Domain.ValueObjects.Money.Money.Create(amount, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(amount);
        result.Value.Currency.Should().Be(currency);
    }
}