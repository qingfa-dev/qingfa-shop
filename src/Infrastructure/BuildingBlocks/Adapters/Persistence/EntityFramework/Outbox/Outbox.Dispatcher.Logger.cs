namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>Structured log events for outbox dispatch.</summary>
internal static partial class OutboxDispatcherLoggers
{
    [LoggerMessage(
        EventId = 230,
        Level = LogLevel.Error,
        Message = "Outbox {Kind} message {MessageId} of type {MessageType} (attempt {Attempts}) could not be published: {Reason}.")]
    public static partial void PublishFailed(
        ILogger logger,
        Guid messageId,
        Application.BuildingBlocks.Messaging.Outbox.OutboxMessageKind kind,
        string messageType,
        int attempts,
        string reason);

    [LoggerMessage(
        EventId = 231,
        Level = LogLevel.Warning,
        Message = "Outbox {Kind} message {MessageId} of type {MessageType} will retry (attempt {Attempts}) in {DelaySeconds} seconds.")]
    public static partial void Retrying(
        ILogger logger,
        Guid messageId,
        Application.BuildingBlocks.Messaging.Outbox.OutboxMessageKind kind,
        string messageType,
        int attempts,
        double delaySeconds);

    [LoggerMessage(
        EventId = 232,
        Level = LogLevel.Error,
        Message = "Outbox {Kind} message {MessageId} of type {MessageType} was dead-lettered after {Attempts} attempts.")]
    public static partial void DeadLettered(
        ILogger logger,
        Guid messageId,
        Application.BuildingBlocks.Messaging.Outbox.OutboxMessageKind kind,
        string messageType,
        int attempts);

    [LoggerMessage(
        EventId = 233,
        Level = LogLevel.Warning,
        Message = "Outbox message {MessageId} could not be marked: {ErrorCode}.")]
    public static partial void MarkFailed(ILogger logger, Guid messageId, string errorCode);
}
