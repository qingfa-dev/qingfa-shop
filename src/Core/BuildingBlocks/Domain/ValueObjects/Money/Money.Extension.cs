namespace Core.BuildingBlocks.Domain.ValueObjects.Money;

/// <summary>Provides currency-aware arithmetic for <see cref="Money"/> values.</summary>
public static class MoneyExtensions
{
    /// <summary>
    /// Adds two money values of the same currency.
    /// </summary>
    /// <summary>Adds two money values with the same currency.</summary>
    /// <param name="left">The left operand and result currency.</param>
    /// <param name="right">The amount to add.</param>
    /// <returns>The sum, or a currency-mismatch error.</returns>
    public static Result<Money> Add(
        this Money left,
        Money right)
    {
        return Result<Money>.Success(left)
            .Ensure(
                predicate: _ => left.Currency.Code == right.Currency.Code,
                error: MoneyOutcome.Failure.CurrencyMismatch)

            .Map(money =>
                new Money(money.Amount + right.Amount, money.Currency));
    }

    /// <summary>
    /// Subtracts two money values of the same currency.
    /// </summary>
    /// <summary>Subtracts the right operand from the left when currencies match.</summary>
    /// <param name="left">The minuend and result currency.</param>
    /// <param name="right">The amount to subtract.</param>
    /// <returns>The difference, or a currency-mismatch error.</returns>
    public static Result<Money> Subtract(
        this Money left,
        Money right)
    {
        return Result<Money>.Success(left)
            .Ensure(
                predicate: _ => left.Currency.Code == right.Currency.Code,
                error: MoneyOutcome.Failure.CurrencyMismatch)

            .Map(money =>
                new Money(money.Amount - right.Amount, money.Currency));
    }

    /// <summary>
    /// Scales the money value; the currency is preserved.
    /// </summary>
    /// <summary>Scales an amount while retaining its currency.</summary>
    /// <param name="money">The amount to scale.</param>
    /// <param name="factor">The multiplicative factor.</param>
    /// <returns>The scaled money value.</returns>
    public static Money Multiply(this Money money, decimal factor)
    {
        return new Money(money.Amount * factor, money.Currency);
    }
}