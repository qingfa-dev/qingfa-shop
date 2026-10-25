namespace Core.Common.Domain.ValueObjects.Localization;

/// <summary>A trimmed, validated language code using two or three lowercase letters.</summary>
/// <param name="Code">The language code created by <see cref="Create"/>.</param>
public readonly partial record struct Language(string? Code)
{
    /// <summary>Creates a language code after trimming and validating its syntax.</summary>
    /// <param name="code">The candidate language code; surrounding whitespace is removed.</param>
    /// <returns>The normalized code, or a validation error.</returns>
    public static Result<Language> Create(string? code)
    {
        var candidate = new Language(code?.Trim());

        return LanguageValidator.Validate(candidate);
    }
}