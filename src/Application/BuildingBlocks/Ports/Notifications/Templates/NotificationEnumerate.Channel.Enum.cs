namespace Application.BuildingBlocks.Ports.Notifications.Templates;

/// <summary>
/// Specifies the delivery channel for a notification.
/// </summary>
public enum NotificationChannel
{
    /// <summary>No delivery channel selected. Primarily for drafts or disabled templates. Also used as the development-fallback target by the unified LoggingProvider.</summary>
    None = 0,

    /// <summary>Delivery via Email provider.</summary>
    Email = 1,

    /// <summary>Delivery via SMS (Text Message) provider.</summary>
    SMS = 2,

    /// <summary>Delivery via Mobile Push Notifications.</summary>
    PushNotification = 3,

    /// <summary>Delivery via WhatsApp business API.</summary>
    WhatsApp = 4
}
