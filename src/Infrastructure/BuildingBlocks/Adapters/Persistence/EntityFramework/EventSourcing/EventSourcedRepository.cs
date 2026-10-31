using System.Diagnostics.CodeAnalysis;

using Application.BuildingBlocks.EventSourcing;
using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Messaging.Serialization;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

using Core.BuildingBlocks.Domain.EventSourcing;
using Core.BuildingBlocks.Functional.Maybes;
using Core.BuildingBlocks.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.EventSourcing;

/// <summary>Loads and saves an event-sourced aggregate against the event store.</summary>
[ExcludeFromCodeCoverage]
public sealed class EventSourcedRepository<TAggregate, TKey>(
    IEventStore eventStore,
    ISnapshotStore snapshotStore,
    ISnapshotPolicy snapshotPolicy,
    IAggregateFactory<TAggregate> aggregateFactory,
    IDomainEventSerializer serializer,
    IOutboxStore outboxStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<EventSourcedRepository<TAggregate, TKey>> logger)
    : IEventSourcedRepository<TAggregate, TKey>
    where TAggregate : class, IEventSourcedAggregate<TKey>
{
    public async Task<Result<TAggregate>> LoadAsync(
        TKey id,
        CancellationToken cancellationToken = default)
    {
        if (id is null)
            return Result<TAggregate>.Fail(EventSourcingOutcome.Failure.StreamNotFound("null"));

        string streamId = EventStreamName.For<TAggregate>(id.ToString()!);
        Result<Maybe<Snapshot>> snapshot = await snapshotStore.LoadAsync(streamId, cancellationToken);
        if (snapshot.IsFailure)
            return Result<TAggregate>.Fail(snapshot.Errors);

        Snapshot? snapshotValue = snapshot.Value.HasValue ? snapshot.Value.Value : null;
        long fromVersion = snapshotValue is null ? 1 : snapshotValue.Version + 1;
        Result<IReadOnlyList<StoredEvent>> stream = await eventStore.ReadStreamAsync(
            streamId,
            fromVersion,
            cancellationToken);

        if (stream.IsFailure)
            return Result<TAggregate>.Fail(stream.Errors);

        if (snapshotValue is null && stream.Value.Count == 0)
            return Result<TAggregate>.Fail(EventSourcingOutcome.Failure.StreamNotFound(streamId));

        TAggregate aggregate = aggregateFactory.Create();

        if (snapshotValue is not null && aggregate is ISnapshotable snapshotable)
        {
            try
            {
                snapshotable.RestoreSnapshot(snapshotValue.State);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Boundary: a corrupt snapshot falls back to a full replay below.
                logger.LogWarning(
                    exception,
                    "Snapshot for {StreamId} could not be restored; replaying the full stream.",
                    streamId);
                snapshotValue = null;
                fromVersion = 1;
                stream = await eventStore.ReadStreamAsync(streamId, 1, cancellationToken);
                if (stream.IsFailure)
                    return Result<TAggregate>.Fail(stream.Errors);
            }
        }

        long version = stream.Value.Count > 0
            ? stream.Value[^1].Version
            : snapshotValue?.Version ?? 0;

        Result loaded = aggregate.TryLoadFromHistory(
            version,
            stream.Value.Select(ToDomainEvent).ToList());

        return loaded.IsFailure
            ? Result<TAggregate>.Fail(loaded.Errors)
            : Result<TAggregate>.Success(aggregate);
    }

    public async Task<Result> SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        IReadOnlyList<IDomainEvent> events = aggregate.UncommittedEvents;
        if (events.Count == 0)
            return Result.Ok();

        string streamId = EventStreamName.For<TAggregate>(aggregate.Id?.ToString() ?? string.Empty);
        long expectedVersion = aggregate.Version - events.Count;

        Result<long> appended = await eventStore.AppendAsync(
            streamId,
            expectedVersion,
            events,
            cancellationToken);

        if (appended.IsFailure)
            return Result.Fail(appended.Errors);

        List<OutboxMessage> messages = [.. events.Select(CreateOutboxMessage)];
        Result staged = await outboxStore.EnqueueAsync(messages, cancellationToken);
        if (staged.IsFailure)
            return staged;

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
            return saved;

        aggregate.ClearUncommittedEvents();

        if (snapshotPolicy.ShouldSnapshot(aggregate.Version, events.Count) &&
            aggregate is ISnapshotable snapshotable)
        {
            Result snapshot = await snapshotStore.SaveAsync(
                new Snapshot(
                    streamId,
                    aggregate.Version,
                    snapshotable.CaptureSnapshot(),
                    timeProvider.GetUtcNow()),
                cancellationToken);

            if (snapshot.IsFailure)
                logger.LogWarning("Snapshot for {StreamId} was not saved.", streamId);
        }

        return Result.Ok();
    }

    private IDomainEvent ToDomainEvent(StoredEvent stored) =>
        serializer.Deserialize(stored.EventName, stored.EventVersion, stored.Payload);

    private OutboxMessage CreateOutboxMessage(IDomainEvent domainEvent)
    {
        DateTimeOffset occurredOnUtc = domainEvent.GetTimestamp() ?? timeProvider.GetUtcNow();
        return new OutboxMessage
        {
            Id = domainEvent.GetEventId() ?? Guid.NewGuid(),
            Kind = OutboxMessageKind.Domain,
            Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.EventName,
            Version = domainEvent.EventVersion,
            Payload = serializer.Serialize(domainEvent),
            Metadata = serializer.Serialize(domainEvent.Metadata),
            OccurredOnUtc = occurredOnUtc,
            AvailableOnUtc = occurredOnUtc,
            CorrelationId = domainEvent.GetCorrelationId(),
            CausationId = domainEvent.GetCausationId(),
            TenantId = domainEvent.GetTenantId(),
        };
    }
}
