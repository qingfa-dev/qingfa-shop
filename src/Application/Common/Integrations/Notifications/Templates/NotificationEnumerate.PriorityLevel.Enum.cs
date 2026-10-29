namespace Application.Common.Integrations.Notifications.Templates;

/// <summary>
/// Defines the urgency of a notification, which determines its background processing queue priority.
/// </summary>
public enum NotificationPriorityLevel
{
    /// <summary>Immediate delivery required (e.g., security alerts, 2FA OTP).</summary>
    Critical,

    /// <summary>High importance, should be processed before standard messages (e.g., failed payments).</summary>
    High,

    /// <summary>Standard transactional messages (e.g., order confirmations).</summary>
    Normal,

    /// <summary>Non-urgent messages (e.g., newsletters, marketing).</summary>
    Low
}
