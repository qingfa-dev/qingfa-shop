using Application.Common.Messaging.Bus;
using Application.Common.Messaging.Outbox;
using Application.Common.Messaging.Serialization;
using Application.Common.Persistence.UnitOfWorks;

using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.EntityFramework.Outbox;

/// <summary>
/// Stages integration events in the outbox instead of publishing them immediately.
/// </summary>
/// <remarks>
/// The caller's unit of work commits the staged row with its own state change. The outbox
/// dispatcher publishes it later, so a handler cannot bypass durability by calling the bus.
/// </remarks>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly IIntegrationEventSerializer _serializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OutboxIntegrationEventPublisher> _logger;

    /// <summary>Creates an outbox-backed integration-event publisher.</summary>
    /// <param name="outboxStore">The store that stages the row.</param>
    /// <param name="serializer">The serializer for the event and its metadata.</param>
    /// <param name="unitOfWork">The unit of work that commits the staged row.</param>
    /// <param name="logger">The logger for staging diagnostics.</param>
    public OutboxIntegrationEventPublisher(
        IOutboxStore outboxStore,
        IIntegrationEventSerializer serializer,
        IUnitOfWork unitOfWork,
        ILogger<OutboxIntegrationEventPublisher> logger)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message = new OutboxMessage
        {
            Id = integrationEvent.MessageId,
            Kind = OutboxMessageKind.Integration,
            Type = integrationEvent.EventName,
            Version = integrationEvent.Version,
            Payload = _serializer.Serialize(integrationEvent),
            Metadata = _serializer.Serialize(integrationEvent.Metadata),
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            AvailableOnUtc = integrationEvent.OccurredOnUtc,
            CorrelationId = integrationEvent.GetCorrelationId(),
            CausationId = integrationEvent.GetCausationId(),
            TenantId = integrationEvent.GetTenantId(),
        };

        Result staged = await _outboxStore
            .EnqueueAsync([message], cancellationToken)
            .ConfigureAwait(false);

        if (staged.IsFailure)
            return staged;

        Result saved = await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (saved.IsFailure)
            return saved;

        OutboxPublisherLoggers.Staged(_logger, integrationEvent.EventName, integrationEvent.MessageId);
        return Result.Ok();
    }
}
