namespace Core.BuildingBlocks.Domain.ValueObjects.Money;

/// <summary>An amount denominated in a currency.</summary>
/// <param name="Amount">The monetary amount.</param>
/// <param name="Currency">The currency in which the amount is denominated.</param>
public readonly partial record struct Money(decimal Amount, Currency Currency)
{
    /// <summary>Creates a money value when its currency code is valid.</summary>
    /// <param name="amount">The amount to retain.</param>
    /// <param name="currency">The currency to validate.</param>
    /// <returns>The money value, or a currency validation error.</returns>
    public static Result<Money> Create(
        decimal amount,
        Currency currency)
    {
        var candidate = new Money(amount, currency);

        return MoneyValidator.Validate(candidate);
    }
}