using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Application.BuildingBlocks.Ports.Notifications.Hubs;

/// <summary>Error definitions for the notification delivery hub.</summary>
public static class NotificationHubOutcome
{
    /// <summary>Builds the stable codes for hub failures.</summary>
    public static class Codes
    {
        /// <summary>Code for a channel without providers.</summary>
        /// <param name="channel">The delivery channel.</param>
        /// <returns>The dotted error code.</returns>
        public static string NoProvidersConfigured(NotificationChannel channel) =>
            $"Notification.{channel}.NoProvidersConfigured";

        /// <summary>Code for a channel whose providers all failed.</summary>
        /// <param name="channel">The delivery channel.</param>
        /// <returns>The dotted error code.</returns>
        public static string AllProvidersFailed(NotificationChannel channel) =>
            $"Notification.{channel}.AllProvidersFailed";
    }

    /// <summary>Notification hub error factories.</summary>
    public static class Failure
    {
        /// <summary>No providers are configured for the requested delivery channel.</summary>
        public static Error NoProvidersConfigured(
            NotificationChannel channel)
            => Error.Unexpected(
                code: Codes.NoProvidersConfigured(channel),
                message: $"No active {channel} providers are configured.");

        /// <summary>All configured providers failed to deliver the message.</summary>
        public static Error AllProvidersFailed(
            NotificationChannel channel)
            => Error.Unexpected(
                code: Codes.AllProvidersFailed(channel),
                message: $"All {channel} providers failed.");
    }
}