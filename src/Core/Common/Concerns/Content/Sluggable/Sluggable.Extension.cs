using System.Globalization;
using System.Text;

namespace Core.Common.Concerns.Content.Sluggable;

/// <summary>Extension methods for the <see cref="ISluggable"/> concern.</summary>
public static class SluggableExtensions
{
    #region Public Methods

    /// <summary>
    /// Sets the slug from an already slugified value.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the sluggable entity.</typeparam>
    /// <param name="result">The result containing the sluggable entity.</param>
    /// <param name="slug">The slug value.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> SetSlug<TValue>(
        this Result<TValue> result,
        string? slug)
        where TValue : ISluggable
    {
        return result
            .Bind(entity => SluggableValidator.ValidateSlug(entity, slug))
            .Tap(entity =>
            {
                entity.Slug = slug!;
            });
    }

    /// <summary>
    /// Slugifies free text (lowercase, diacritics folded, non-alphanumerics
    /// collapsed to single hyphens) and sets it as the slug.
    /// </summary>
    /// <typeparam name="TValue">The type of the sluggable entity.</typeparam>
    /// <param name="result">The result containing the sluggable entity.</param>
    /// <param name="text">The free text to slugify.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> SetSlugFromText<TValue>(
        this Result<TValue> result,
        string? text)
        where TValue : ISluggable
    {
        return result.SetSlug(Slugify(text));
    }

    /// <summary>
    /// Converts free text to a slug by lowercasing, folding diacritics,
    /// and collapsing non-alphanumerics to single hyphens.
    /// </summary>
    /// <param name="text">The text to slugify.</param>
    /// <returns>The slugified string.</returns>
    public static string Slugify(string? text)
    {
        // Guard: Return empty for null or whitespace input.
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Compute: Normalize to lowercase with diacritic folding.
        var normalized = text
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            // Compute: Keep alphanumeric characters.
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                builder.Append(character);
            }
            // Fallback: Insert hyphen between word segments.
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        // Compute: Trim trailing hyphens.
        return builder.ToString().Trim('-');
    }

    #endregion
}