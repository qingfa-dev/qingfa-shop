namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Structured log events for outbox-backed integration publication.</summary>
internal static partial class OutboxPublisherLoggers
{
    [LoggerMessage(
        EventId = 221,
        Level = LogLevel.Debug,
        Message = "Staged integration event {EventName} ({MessageId}) in the outbox.")]
    public static partial void Staged(ILogger logger, string eventName, Guid messageId);
}
