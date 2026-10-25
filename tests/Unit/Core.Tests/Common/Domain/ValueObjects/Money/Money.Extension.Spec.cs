using Core.Common.Domain.ValueObjects.Money;

namespace Core.Tests.Domain.ValueObjects.Money;

public class MoneyExtensionSpec
{
    private static global::Core.Common.Domain.ValueObjects.Money.Money Usd(decimal amount)
        => global::Core.Common.Domain.ValueObjects.Money.Money.Create(amount, Currency.Create("USD").Value).Value;

    private static global::Core.Common.Domain.ValueObjects.Money.Money Eur(decimal amount)
        => global::Core.Common.Domain.ValueObjects.Money.Money.Create(amount, Currency.Create("EUR").Value).Value;

    [Fact]
    public void Add_SameCurrency_ShouldSumAmountsAndPreserveCurrency()
    {
        // Arrange
        var left = Usd(10m);
        var right = Usd(5m);

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(15m);
        result.Value.Currency.Code.Should().Be("USD");
    }

    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(10, -3, 7)]
    [InlineData(-5, -5, -10)]
    public void Add_SameCurrency_WithZeroOrNegative_ShouldSucceed(decimal leftAmount, decimal rightAmount, decimal expected)
    {
        // Arrange
        var left = Usd(leftAmount);
        var right = Usd(rightAmount);

        // Act
        var result = left.Add(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(expected);
        result.Value.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Add_MismatchedCurrencies_ShouldFailWithMismatch()
    {
        // Arrange
        var left = Usd(10m);
        var right = Eur(5m);

        // Act
        var result = left.Add(right);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Money.Currency.Mismatch");
    }

    [Fact]
    public void Subtract_SameCurrency_ShouldReturnDifferenceAndPreserveCurrency()
    {
        // Arrange
        var left = Usd(10m);
        var right = Usd(4m);

        // Act
        var result = left.Subtract(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(6m);
        result.Value.Currency.Code.Should().Be("USD");
    }

    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(10, -3, 13)]
    [InlineData(-5, -5, 0)]
    public void Subtract_SameCurrency_WithZeroOrNegative_ShouldSucceed(decimal leftAmount, decimal rightAmount, decimal expected)
    {
        // Arrange
        var left = Usd(leftAmount);
        var right = Usd(rightAmount);

        // Act
        var result = left.Subtract(right);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(expected);
        result.Value.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Subtract_MismatchedCurrencies_ShouldFailWithMismatch()
    {
        // Arrange
        var left = Usd(10m);
        var right = Eur(4m);

        // Act
        var result = left.Subtract(right);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Money.Currency.Mismatch");
    }

    [Theory]
    [InlineData(10, 2, 20)]
    [InlineData(10, 0, 0)]
    [InlineData(10, -1.5, -15)]
    public void Multiply_ByFactor_ShouldScaleAmountAndPreserveCurrency(decimal amount, decimal factor, decimal expected)
    {
        // Arrange
        var money = Usd(amount);

        // Act
        var result = money.Multiply(factor);

        // Assert
        result.Amount.Should().Be(expected);
        result.Currency.Code.Should().Be("USD");
    }
}