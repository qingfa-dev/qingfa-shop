using System.Globalization;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

namespace Infrastructure.Modules.Payments.Adapters.Shared;

/// <summary>Converts domain money to the units a provider expects.</summary>
public static class MoneyConverter
{
    /// <summary>The currencies a provider expects without minor units.</summary>
    private static readonly HashSet<string> ZeroDecimal =
        ["JPY", "KRW", "VND", "CLP", "ISK", "HUF", "TWD"];

    /// <summary>The currencies a provider expects with three decimal places.</summary>
    private static readonly HashSet<string> ThreeDecimal = ["BHD", "JOD", "KWD", "OMR", "TND"];

    /// <summary>Returns the currency exponent.</summary>
    /// <param name="currency">The currency code.</param>
    /// <returns>The number of decimal places.</returns>
    public static int Exponent(string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return ZeroDecimal.Contains(currency)
            ? 0
            : ThreeDecimal.Contains(currency)
                ? 3
                : 2;
    }

    /// <summary>Converts to the provider's minor units.</summary>
    /// <param name="money">The amount.</param>
    /// <returns>The minor units, or a stable failure when the amount has too many decimals.</returns>
    public static Result<long> ToMinorUnits(Money money)
    {
        int exponent = Exponent(money.Currency.Code ?? PaymentConstant.Defaults.Currency);
        decimal scaled = money.Amount * (decimal)Math.Pow(MoneyConverterConstant.DecimalBase, exponent);

        if (scaled != decimal.Truncate(scaled))
            return Result<long>.Fail(PaymentsOutcome.Failure.AmountInvalid(
                $"the currency allows {exponent} decimal places"));

        return Result<long>.Success((long)scaled);
    }

    /// <summary>Converts from the provider's minor units.</summary>
    /// <param name="amount">The minor units.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>The domain amount.</returns>
    public static Money FromMinorUnits(long amount, Currency currency)
        => new(amount / (decimal)Math.Pow(10, Exponent(currency.Code ?? PaymentConstant.Defaults.Currency)), currency);

    /// <summary>Formats an amount for a provider that takes decimal strings.</summary>
    /// <param name="money">The amount.</param>
    /// <returns>The invariant decimal string.</returns>
    public static string ToDecimalString(Money money)
        => money.Amount.ToString(
            $"F{Exponent(money.Currency.Code ?? PaymentConstant.Defaults.Currency)}",
            CultureInfo.InvariantCulture);
}
