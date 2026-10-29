using Infrastructure.Integrations.Notifications.Providers;

namespace Infrastructure.Integrations.Notifications.Channels.Logging.Providers;

/// <summary>Provides a logging-only notification provider for development and testing environments. Always succeeds after logging the attempt.</summary>
public sealed partial class LoggingProvider(ILogger<LoggingProvider> logger, NotificationChannel channel)
    : INotificationProvider
{
    public string Name => "Logging";

    public int Priority => int.MaxValue;

    public bool IsEnabled => true;

    public NotificationChannel Channel => channel;

    public Task<Result> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            NotificationPriorityLevel priorityLevel = NotificationStore.Templates.TryGetValue(message.UseCase, out NotificationTemplate? template)
                ? template.Priority
                : NotificationPriorityLevel.Normal;

            string language = message.Metadata.TryGetValue(NotificationMetadata.Language, out var langValue)
                ? langValue?.ToString() ?? "en"
                : "en";

            // Log: Record outbound notification attempt with channel metadata
            NotificationProvider.Loggers.LogSending(
                logger,
                Name,
                Channel,
                priorityLevel,
                language,
                message.Recipient.Identifier);

            // Log: Record successful delivery (dev fallback — no actual send)
            NotificationProvider.Loggers.LogSendSuccess(logger, Name, message.Recipient.Identifier);

            return Task.FromResult(Result.Ok());
        }
        // Catch: Swallow exception and return degraded failure result
        catch (Exception ex)
        {
            NotificationProvider.Loggers.LogSendException(logger, Name, ex);
            // Degrade: Return failure result without rethrowing — non-critical provider
            return Task.FromResult(
                Result.Fail(NotificationProviderResult.Failure.SendFailed(Name, ex.Message)));
        }
    }
}
