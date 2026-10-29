namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>Structured log events for outbox staging.</summary>
internal static partial class OutboxInterceptorLoggers
{
    [LoggerMessage(
        EventId = 220,
        Level = LogLevel.Debug,
        Message = "Staged {Count} domain event(s) in the outbox.")]
    public static partial void Staged(ILogger logger, int count);
}
