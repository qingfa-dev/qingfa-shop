namespace Application.BuildingBlocks.Ports.Notifications.Templates;

/// <summary>
/// Specifies the structural format of a notification template.
/// </summary>
public enum NotificationFormat
{
    /// <summary>Plain text format without styling. Best for SMS and basic alerts.</summary>
    Default,

    /// <summary>Rich HTML format. Best for branded marketing and transactional emails.</summary>
    Html
}