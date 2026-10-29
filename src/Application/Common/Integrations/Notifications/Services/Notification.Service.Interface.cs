using Application.Common.Integrations.Notifications.Models;

namespace Application.Common.Integrations.Notifications.Services;

/// <summary>
/// Defines the core orchestration service for sending notifications across different channels.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Unified send method that dispatches a notification message based on its configured use case and recipient.
    /// </summary>
    /// <param name="message">The high-level notification request containing use case, recipient, and context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or a list of failures.</returns>
    Task<Result> SendAsync(NotificationMessage message, CancellationToken ct = default);

    /// <summary>
    /// The actual delivery logic that dispatches the message to channel hubs. 
    /// This is typically called by background workers or directly when backgrounding is disabled.
    /// </summary>
    Task<Result> SendInternalAsync(NotificationMessage message, CancellationToken ct = default);
}
