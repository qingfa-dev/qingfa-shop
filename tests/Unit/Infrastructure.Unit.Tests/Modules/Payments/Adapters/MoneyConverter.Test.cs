using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;

using Infrastructure.Modules.Payments.Adapters.Shared;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class MoneyConverterTest
{
    [Theory]
    [InlineData("USD", 10.25, 1025L)]
    [InlineData("EUR", 0.99, 99L)]
    [InlineData("JPY", 1000, 1000L)]
    [InlineData("KRW", 5000, 5000L)]
    [InlineData("KWD", 1.234, 1234L)]
    [InlineData("BHD", 2.5, 2500L)]
    public void ToMinorUnits_ShouldRespectTheCurrencyExponent(string currency, double amount, long expected)
        => MoneyConverter.ToMinorUnits(new Money((decimal)amount, new Currency(currency)))
            .Value.Should().Be(expected);

    [Fact]
    public void ToMinorUnits_TooManyDecimals_ShouldFail()
    {
        var result = MoneyConverter.ToMinorUnits(new Money(10.001m, new Currency("USD")));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
    }

    [Fact]
    public void FromMinorUnits_ShouldRoundTrip()
    {
        Money money = MoneyConverter.FromMinorUnits(1025L, new Currency("USD"));
        money.Amount.Should().Be(10.25m);
    }

    [Theory]
    [InlineData("USD", 10.5, "10.50")]
    [InlineData("JPY", 1000, "1000")]
    [InlineData("KWD", 1.5, "1.500")]
    public void ToDecimalString_ShouldUseTheCurrencyExponent(string currency, double amount, string expected)
        => MoneyConverter.ToDecimalString(new Money((decimal)amount, new Currency(currency)))
            .Should().Be(expected);

    [Fact]
    public void Exponent_ShouldDefaultToTwo()
        => MoneyConverter.Exponent("GBP").Should().Be(2);
}
