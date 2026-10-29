using Application.Common.Integrations.Notifications.Templates;

namespace Application.Common.Integrations.Notifications.Models;

/// <summary>
/// Represents a key-value pair used to populate placeholders in a notification template.
/// </summary>
public sealed record NotificationParameter
{
    /// <summary>The parameter type.</summary>
    public NotificationParameterType Key { get; init; }

    /// <summary>The string value to inject.</summary>
    public string? Value { get; init; }

    public bool IsRequired { get; init; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="NotificationParameter"/>.
    /// </summary>
    /// <param name="key">The parameter type.</param>
    /// <param name="value">The string value.</param>
    /// <param name="isRequired">Whether the parameter is required.</param>
    /// <returns>A new <see cref="NotificationParameter"/>.</returns>
    public static NotificationParameter Create(
        NotificationParameterType key,
        string? value,
        bool isRequired = true) => new() { Key = key, Value = value, IsRequired = isRequired };
}