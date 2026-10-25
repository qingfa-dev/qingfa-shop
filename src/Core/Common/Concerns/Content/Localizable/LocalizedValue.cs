namespace Core.Common.Concerns.Content.Localizable;

/// <summary>Represents a single localized value for a property and locale.</summary>
public sealed class LocalizedValue
{
    #region Properties

    /// <summary>Gets or sets the property name.</summary>
    public string Property { get; set; } = null!;
    /// <summary>Gets or sets the locale identifier.</summary>
    public string Locale { get; set; } = null!;
    /// <summary>Gets or sets the localized value.</summary>
    public string? Value { get; set; }

    #endregion
}