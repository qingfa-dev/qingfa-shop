using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Domain.ValueObjects.Money;

/// <summary>Validates currency codes without modifying the supplied value.</summary>
public static class CurrencyValidator
{
    /// <summary>
    /// Validates a currency without mutating it.
    /// </summary>
    /// <summary>Returns success when the code contains exactly three uppercase letters.</summary>
    /// <param name="currency">The currency to validate.</param>
    /// <returns>The unchanged currency on success, or its validation errors.</returns>
    public static Result<Currency> Validate(Currency currency)
    {
        return Result<Currency>.Success(currency)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(currency.Code),
                error: CurrencyOutcome.Failure.CodeRequired)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        currency.Code!,
                        CurrencyConstant.Patterns.Code),
                error: CurrencyOutcome.Failure.CodeInvalid);
    }
}