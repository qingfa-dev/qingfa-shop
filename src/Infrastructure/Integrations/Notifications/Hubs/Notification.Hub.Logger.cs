namespace Infrastructure.Integrations.Notifications.Hubs;

public partial class NotificationHub
{
    /// <summary>Structured log event definitions for the notification delivery hub. Uses source-generated LoggerMessage for high-performance logging.</summary>
    internal static partial class Loggers
    {
        /// <summary>Logs that no active providers are configured for the given channel.</summary>
        [LoggerMessage(
            EventId = 230,
            Level = LogLevel.Warning,
            Message = "No active providers configured for channel {Channel}")]
        public static partial void LogNoActiveProviders(
            ILogger logger,
            NotificationChannel channel);

        /// <summary>Logs an attempt to send via a specific provider with its priority.</summary>
        [LoggerMessage(
            EventId = 231,
            Level = LogLevel.Information,
            Message = "Attempting notification via {Provider}. Channel: {Channel}, Priority: {Priority}, Recipient: {Recipient}")]
        public static partial void LogAttemptingToSend(
            ILogger logger,
            string provider,
            NotificationChannel channel,
            int priority,
            string recipient);

        /// <summary>Logs successful delivery through a provider.</summary>
        [LoggerMessage(
            EventId = 232,
            Level = LogLevel.Information,
            Message = "Notification sent successfully via {Provider}. Channel: {Channel}, Recipient: {Recipient}")]
        public static partial void LogSendSuccess(
            ILogger logger,
            string provider,
            NotificationChannel channel,
            string recipient);

        /// <summary>Logs that a specific provider failed to deliver.</summary>
        [LoggerMessage(
            EventId = 233,
            Level = LogLevel.Warning,
            Message = "Provider {Provider} failed. Channel: {Channel}, Error: {Error}")]
        public static partial void LogProviderFailed(
            ILogger logger,
            string provider,
            NotificationChannel channel,
            string? error);

        /// <summary>Logs that all providers for a channel have been exhausted.</summary>
        [LoggerMessage(
            EventId = 234,
            Level = LogLevel.Error,
            Message = "All providers failed. Channel: {Channel}, Recipient: {Recipient}")]
        public static partial void LogAllProvidersFailed(
            ILogger logger,
            NotificationChannel channel,
            string recipient);
    }
}