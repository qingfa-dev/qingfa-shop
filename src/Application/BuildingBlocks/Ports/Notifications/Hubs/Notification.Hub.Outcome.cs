using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Application.BuildingBlocks.Ports.Notifications.Hubs;

/// <summary>Error definitions for the notification delivery hub.</summary>
public static class NotificationHubOutcome
{
    /// <summary>Notification hub error factories.</summary>
    public static class Failure
    {
        /// <summary>No providers are configured for the requested delivery channel.</summary>
        public static Error NoProvidersConfigured(
            NotificationChannel channel)
            => Error.Unexpected(
                code: $"Notification.{channel}.NoProvidersConfigured",
                message: $"No active {channel} providers are configured.");

        /// <summary>All configured providers failed to deliver the message.</summary>
        public static Error AllProvidersFailed(
            NotificationChannel channel)
            => Error.Unexpected(
                code: $"Notification.{channel}.AllProvidersFailed",
                message: $"All {channel} providers failed.");
    }
}