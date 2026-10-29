using Application.Common.Integrations.Notifications.Templates;

namespace Infrastructure.Integrations.Notifications.Providers;

public partial class NotificationProvider
{
    /// <summary>Structured log event definitions for notification providers. Uses source-generated LoggerMessage for high-performance logging.</summary>
    internal static partial class Loggers
    {
        /// <summary>Logs that a provider is attempting to send a notification with channel and priority metadata.</summary>
        [LoggerMessage(
            EventId = 236,
            Level = LogLevel.Information,
            Message = "Sending notification via {Provider}. Channel: {Channel}, Priority: {Priority}, Language: {Language}, Recipient: {Recipient}")]
        public static partial void LogSending(
            ILogger logger,
            string provider,
            NotificationChannel channel,
            NotificationPriorityLevel priority,
            string language,
            string recipient);

        /// <summary>Logs that a provider failed with the given error message.</summary>
        [LoggerMessage(
            EventId = 237,
            Level = LogLevel.Error,
            Message = "Failed sending notification via {Provider}. Errors: {Errors}")]
        public static partial void LogSendFailed(
            ILogger logger,
            string provider,
            string errors);

        /// <summary>Logs successful delivery through a provider.</summary>
        [LoggerMessage(
            EventId = 238,
            Level = LogLevel.Information,
            Message = "Notification sent successfully via {Provider} to {Recipient}")]
        public static partial void LogSendSuccess(
            ILogger logger,
            string provider,
            string recipient);

        /// <summary>Logs an unexpected exception during provider send.</summary>
        [LoggerMessage(
            EventId = 239,
            Level = LogLevel.Error,
            Message = "Exception while sending notification via {Provider}")]
        public static partial void LogSendException(
            ILogger logger,
            string provider,
            Exception exception);
    }
}