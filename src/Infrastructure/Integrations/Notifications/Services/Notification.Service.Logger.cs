using Application.Common.Integrations.Notifications.Templates;

namespace Infrastructure.Integrations.Notifications.Services;

public partial class NotificationService
{
    /// <summary>Structured log event definitions for notification orchestration.</summary>
    internal static partial class Loggers
    {
        [LoggerMessage(
            EventId = 253,
            Level = LogLevel.Information,
            Message = "Enqueuing notification job. UseCase: {UseCase}, Priority: {Priority}, Queue: {Queue}")]
        public static partial void LogEnqueuingNotificationJob(ILogger logger, NotificationUseCase useCase, NotificationPriorityLevel priority, string queue);

        [LoggerMessage(
            EventId = 254,
            Level = LogLevel.Information,
            Message = "Processing notification delivery. UseCase: {UseCase}, Recipient: {Recipient}")]
        public static partial void LogProcessingNotificationDelivery(ILogger logger, NotificationUseCase useCase, string recipient);

        [LoggerMessage(
            EventId = 255,
            Level = LogLevel.Debug,
            Message = "Notification handed off to delivery pipeline. UseCase: {UseCase}, Recipient: {Recipient}")]
        public static partial void LogHandoffToDelivery(ILogger logger, NotificationUseCase useCase, string recipient);

        [LoggerMessage(
            EventId = 256,
            Level = LogLevel.Information,
            Message = "Background job created for notification dispatch. JobId: {JobId}, UseCase: {UseCase}")]
        public static partial void LogBackgroundJobCreated(ILogger logger, string jobId, NotificationUseCase useCase);

        [LoggerMessage(
            EventId = 257,
            Level = LogLevel.Warning,
            Message = "Template not found for notification. UseCase: {UseCase}, TemplateName: {TemplateName}")]
        public static partial void LogTemplateNotFound(ILogger logger, NotificationUseCase useCase, string templateName);

        [LoggerMessage(
            EventId = 258,
            Level = LogLevel.Error,
            Message = "Unexpected exception during notification processing. UseCase: {UseCase}")]
        public static partial void LogServiceException(ILogger logger, Exception exception, NotificationUseCase useCase);
    }
}
