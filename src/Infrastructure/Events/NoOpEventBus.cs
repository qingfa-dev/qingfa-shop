using Application.Common.Messaging.Bus;

namespace Infrastructure.Events;

/// <summary>
/// A bus that accepts integration events without a transport.
/// </summary>
/// <remarks>
/// Slice ② has no broker. The outbox dispatcher still marks integration rows processed so the
/// pipeline is exercised end to end; slice ③ replaces this registration with Wolverine.
/// </remarks>
public sealed class NoOpEventBus(ILogger<NoOpEventBus> logger) : IEventBus
{
    /// <inheritdoc />
    public Task<Result> PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        NoOpEventBusLoggers.Accepted(logger, integrationEvent.EventName, integrationEvent.MessageId);
        return Task.FromResult(Result.Ok());
    }
}

/// <summary>Structured log events for the no-op bus.</summary>
internal static partial class NoOpEventBusLoggers
{
    [LoggerMessage(
        EventId = 222,
        Level = LogLevel.Debug,
        Message = "No transport is configured; accepting integration event {EventName} ({MessageId}).")]
    public static partial void Accepted(ILogger logger, string eventName, Guid messageId);
}
