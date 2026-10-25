namespace Core.Common.Domain.ValueObjects.Money;

/// <summary>A currency code normalized to uppercase.</summary>
/// <param name="Code">The three-letter uppercase code created by <see cref="Create"/>.</param>
public readonly partial record struct Currency(string? Code)
{
    /// <summary>Creates a currency after trimming, uppercasing, and validating its code.</summary>
    /// <param name="code">The candidate currency code.</param>
    /// <returns>The normalized currency, or a validation error.</returns>
    public static Result<Currency> Create(string? code)
    {
        var candidate = new Currency(code?.Trim().ToUpperInvariant());

        return CurrencyValidator.Validate(candidate);
    }
}