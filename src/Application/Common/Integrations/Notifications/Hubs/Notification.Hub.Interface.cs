using Application.Common.Integrations.Notifications.Models;

namespace Application.Common.Integrations.Notifications.Hubs;

/// <summary>Defines the notification delivery hub that dispatches messages across configured providers with fallback support.</summary>
public interface INotificationHub
{
    /// <summary>Sends a notification message through the first available enabled provider for the message's channel. Providers are tried in priority order; if all fail, the hub returns an aggregate failure.</summary>
    /// <param name="message">The fully-rendered notification message to deliver.</param>
    /// <param name="ct">Cancellation token to abort the delivery attempt.</param>
    /// <returns>A Result indicating success or the failure from the last attempted provider.</returns>
    Task<Result> SendAsync(
        NotificationMessage message,
        CancellationToken ct = default);
}