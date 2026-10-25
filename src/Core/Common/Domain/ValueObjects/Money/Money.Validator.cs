using System.Text.RegularExpressions;

namespace Core.Common.Domain.ValueObjects.Money;

/// <summary>Validates money values without changing their amount or currency.</summary>
public static class MoneyValidator
{
    /// <summary>
    /// Validates a money value without mutating it.
    /// </summary>
    /// <summary>Returns success when the currency code is valid.</summary>
    /// <param name="money">The money value to validate.</param>
    /// <returns>The unchanged value on success, or its validation error.</returns>
    public static Result<Money> Validate(Money money)
    {
        return Result<Money>.Success(money)
            .Ensure(
                predicate: _ =>
                    !string.IsNullOrWhiteSpace(money.Currency.Code) &&
                    Regex.IsMatch(
                        money.Currency.Code!,
                        CurrencyConstant.Patterns.Code),
                error: MoneyOutcome.Failure.CurrencyInvalid);
    }
}