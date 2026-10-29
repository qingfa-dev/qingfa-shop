using System.Text.Json;

namespace Application.Common.Integrations.Notifications.Models;

/// <summary>
/// Encapsulates non-content delivery metadata for a notification request.
/// </summary>
public static class NotificationMetadata
{
    /// <summary>Gets the priority level of the notification.</summary>
    public static readonly string Priority = JsonNamingPolicy.CamelCase.ConvertName(nameof(Priority));

    /// <summary>Gets the preferred language for the content (e.g., "en-US").</summary>
    public static readonly string Language = JsonNamingPolicy.CamelCase.ConvertName(nameof(Language));

    /// <summary>Gets the name of the system or user that initiated the notification.</summary>
    public static readonly string CreatedBy = JsonNamingPolicy.CamelCase.ConvertName(nameof(CreatedBy));

}
