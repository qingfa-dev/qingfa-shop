namespace Core.Common.Concerns.Content.Localizable;

/// <summary>Extension methods for the <see cref="ILocalizable"/> concern.</summary>
public static class LocalizableExtensions
{
    #region Public Methods

    /// <summary>
    /// Sets the value for a (property, locale) pair, adding the entry when new.
    /// Inputs are normalized (trimmed) before validation runs,
    /// so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the localizable entity.</typeparam>
    /// <param name="result">The result containing the localizable entity.</param>
    /// <param name="property">The localized property name.</param>
    /// <param name="locale">The locale identifier.</param>
    /// <param name="value">The localized value to set.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> SetLocalizedValue<TValue>(
        this Result<TValue> result,
        string? property,
        string? locale,
        string? value)
        where TValue : ILocalizable
    {
        var normalizedProperty = NormalizeProperty(property);
        var normalizedLocale = NormalizeLocale(locale);

        return result
            .Bind(entity =>
                LocalizableValidator.ValidateLocalizedValue(
                    entity,
                    normalizedProperty,
                    normalizedLocale,
                    value))

            .Tap(entity =>
            {
                var existing = entity.LocalizedValues.FirstOrDefault(entry =>
                    entry.Property == normalizedProperty &&
                    entry.Locale == normalizedLocale);

                if (existing is null)
                {
                    entity.LocalizedValues.Add(new LocalizedValue
                    {
                        Property = normalizedProperty!,
                        Locale = normalizedLocale!,
                        Value = value
                    });
                }
                else
                {
                    existing.Value = value;
                }
            });
    }

    /// <summary>
    /// Removes the value for a (property, locale) pair.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the localizable entity.</typeparam>
    /// <param name="result">The result containing the localizable entity.</param>
    /// <param name="property">The localized property name.</param>
    /// <param name="locale">The locale identifier.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> RemoveLocalizedValue<TValue>(
        this Result<TValue> result,
        string? property,
        string? locale)
        where TValue : ILocalizable
    {
        var normalizedProperty = NormalizeProperty(property);
        var normalizedLocale = NormalizeLocale(locale);

        return result
            .Bind(entity =>
                LocalizableValidator.ValidateLocalizedValueRemoval(
                    entity,
                    normalizedProperty,
                    normalizedLocale))

            .Tap(entity =>
            {
                var existing = entity.LocalizedValues.First(entry =>
                    entry.Property == normalizedProperty &&
                    entry.Locale == normalizedLocale);

                entity.LocalizedValues.Remove(existing);
            });
    }

    /// <summary>
    /// Removes all localized values from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The type of the localizable entity.</typeparam>
    /// <param name="result">The result containing the localizable entity.</param>
    /// <returns>The result with the entity mutated on success.</returns>
    public static Result<TValue> ClearLocalizedValues<TValue>(
        this Result<TValue> result)
        where TValue : ILocalizable
    {
        return result
            .Bind(entity =>
                LocalizableValidator.ValidateClearLocalizedValues(entity))
            .Tap(entity =>
            {
                entity.LocalizedValues.Clear();
            });
    }

    /// <summary>Gets the value for a (property, locale) pair, if present.</summary>
    /// <typeparam name="TValue">The type of the localizable entity.</typeparam>
    /// <param name="entity">The localizable entity.</param>
    /// <param name="property">The localized property name.</param>
    /// <param name="locale">The locale identifier.</param>
    /// <returns>The localized value, or null when the pair is not present.</returns>
    public static string? GetLocalizedValue<TValue>(
        this TValue entity,
        string? property,
        string? locale)
        where TValue : ILocalizable
    {
        // Guard: Missing entity or inputs cannot match a translation.
        if (entity is null || property is null || locale is null)
        {
            return null;
        }

        return entity.LocalizedValues
            .FirstOrDefault(entry =>
                entry.Property == NormalizeProperty(property) &&
                entry.Locale == NormalizeLocale(locale))
            ?.Value;
    }

    /// <summary>Normalizes a localized property name by trimming whitespace.</summary>
    /// <param name="property">The property name to normalize.</param>
    /// <returns>The normalized property name, or null when the input is null.</returns>
    public static string? NormalizeProperty(string? property)
    {
        return property?.Trim();
    }

    /// <summary>Normalizes a locale identifier by trimming whitespace.</summary>
    /// <param name="locale">The locale identifier to normalize.</param>
    /// <returns>The normalized locale identifier, or null when the input is null.</returns>
    public static string? NormalizeLocale(string? locale)
    {
        return locale?.Trim();
    }

    #endregion
}