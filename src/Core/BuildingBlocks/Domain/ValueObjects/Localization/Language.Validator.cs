using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Domain.ValueObjects.Localization;

/// <summary>Validates language codes without modifying them.</summary>
public static class LanguageValidator
{
    /// <summary>Returns success when a code is present and matches the configured syntax and length.</summary>
    /// <param name="language">The language code to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<Language> Validate(Language language)
    {
        return Result<Language>.Success(language)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(language.Code),
                error: LanguageOutcome.Failure.CodeRequired)

            .Ensure(
                predicate: _ =>
                    language.Code!.Length <=
                    LanguageConstant.Constraints.Code.MaxLength,
                error: LanguageOutcome.Failure.CodeTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        language.Code!,
                        LanguageConstant.Patterns.Code),
                error: LanguageOutcome.Failure.CodeInvalid);
    }
}