using System.Text.RegularExpressions;

namespace Core.Common.Domain.ValueObjects.Localization;

/// <summary>Validates locale codes without modifying them.</summary>
public static class LocaleValidator
{
    /// <summary>Returns success when a code is present and matches the configured syntax and length.</summary>
    /// <param name="locale">The locale code to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<Locale> Validate(Locale locale)
    {
        return Result<Locale>.Success(locale)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(locale.Code),
                error: LocaleResult.Failure.CodeRequired)

            .Ensure(
                predicate: _ =>
                    locale.Code!.Length <=
                    LocaleConstant.Constraints.Code.MaxLength,
                error: LocaleResult.Failure.CodeTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(locale.Code!, LocaleConstant.Patterns.Code),
                error: LocaleResult.Failure.CodeInvalid);
    }
}