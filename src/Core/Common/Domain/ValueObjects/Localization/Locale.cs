namespace Core.Common.Domain.ValueObjects.Localization;

/// <summary>A trimmed, validated locale code.</summary>
/// <param name="Code">The locale code created by <see cref="Create"/>.</param>
public readonly partial record struct Locale(string? Code)
{
    /// <summary>Creates a locale code after trimming and validating its configured syntax.</summary>
    /// <param name="code">The candidate locale code; surrounding whitespace is removed.</param>
    /// <returns>The normalized code, or a validation error.</returns>
    public static Result<Locale> Create(string? code)
    {
        var candidate = new Locale(code?.Trim());

        return LocaleValidator.Validate(candidate);
    }
}