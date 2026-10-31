using Application.BuildingBlocks.Messaging.Outbox;
using Application.BuildingBlocks.Messaging.Serialization;

using Core.BuildingBlocks.Domain.Aggregates;
using Core.BuildingBlocks.Domain.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Outbox;

/// <summary>
/// Stages aggregate events as outbox rows in the same save as the state change.
/// </summary>
/// <remarks>
/// <para>
/// The interceptor writes rows during <see cref="SavingChanges"/> and clears the aggregate
/// event lists only after <see cref="SavedChanges"/>. A failed save never clears them, so a
/// retry can stage the events again.
/// </para>
/// <para>
/// This interceptor is created per <see cref="DbContext"/> because it remembers which event
/// ids it staged during the current save.
/// </para>
/// </remarks>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventSerializer _serializer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly HashSet<Guid> _stagedEventIds = [];
    private readonly List<IAggregate<IDomainEvent>> _stagedAggregates = [];
    private readonly List<OutboxMessage> _stagedMessages = [];

    /// <summary>Creates an outbox interceptor.</summary>
    /// <param name="serializer">The serializer used for event payloads and metadata.</param>
    /// <param name="timeProvider">The fallback clock for events without a timestamp.</param>
    /// <param name="logger">The logger for staging diagnostics.</param>
    public OutboxInterceptor(
        IDomainEventSerializer serializer,
        TimeProvider timeProvider,
        ILogger<OutboxInterceptor>? logger = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? NullLogger<OutboxInterceptor>.Instance;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stage(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stage(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        Clear();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Clear();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DetachStaged(eventData.Context);
        Reset();
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachStaged(eventData.Context);
        Reset();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>Stages one row for every aggregate event not already staged in this save.</summary>
    /// <param name="context">The context being saved.</param>
    private void Stage(DbContext? context)
    {
        if (context is null)
            return;

        // Recover: a previous save may have failed before SaveChangesFailed ran; never orphan its rows.
        DetachStaged(context);

        List<IAggregate<IDomainEvent>> aggregates = context.ChangeTracker
            .Entries<IAggregate<IDomainEvent>>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.Events.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
            return;

        List<OutboxMessage> messages = [];

        foreach (IAggregate<IDomainEvent> aggregate in aggregates)
        {
            foreach (IDomainEvent domainEvent in aggregate.Events)
            {
                Guid eventId = domainEvent.GetEventId() ?? Guid.NewGuid();
                if (domainEvent.GetEventId() is null)
                    domainEvent.SetEventId(eventId);

                if (!_stagedEventIds.Add(eventId))
                    continue;

                messages.Add(CreateMessage(domainEvent, eventId));
            }

            if (!_stagedAggregates.Contains(aggregate))
                _stagedAggregates.Add(aggregate);
        }

        if (messages.Count == 0)
            return;

        // Publish: stage in the current save so the outbox row and state change commit together.
        context.Set<OutboxMessage>().AddRange(messages);
        _stagedMessages.AddRange(messages);
        OutboxInterceptorLoggers.Staged(_logger, messages.Count);
    }

    /// <summary>Builds the outbox row for one domain event.</summary>
    /// <param name="domainEvent">The event to serialize.</param>
    /// <param name="eventId">The event id used as the outbox row id.</param>
    /// <returns>The staged outbox message.</returns>
    private OutboxMessage CreateMessage(IDomainEvent domainEvent, Guid eventId)
    {
        DateTimeOffset occurredOnUtc = domainEvent.GetTimestamp() ?? _timeProvider.GetUtcNow();

        return new OutboxMessage
        {
            Id = eventId,
            Kind = OutboxMessageKind.Domain,
            Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.EventName,
            Version = domainEvent.EventVersion,
            Payload = _serializer.Serialize(domainEvent),
            Metadata = _serializer.Serialize(domainEvent.Metadata),
            OccurredOnUtc = occurredOnUtc,
            AvailableOnUtc = occurredOnUtc,
            CorrelationId = domainEvent.GetCorrelationId(),
            CausationId = domainEvent.GetCausationId(),
            TenantId = domainEvent.GetTenantId(),
        };
    }

    /// <summary>Clears only events that were staged by this interceptor.</summary>
    private void Clear()
    {
        foreach (IAggregate<IDomainEvent> aggregate in _stagedAggregates)
        {
            bool allStaged = aggregate.Events
                .All(domainEvent => domainEvent.GetEventId() is Guid id && _stagedEventIds.Contains(id));

            if (allStaged)
                aggregate.RemoveAllEvents();
        }

        _stagedMessages.Clear();
        Reset();
    }

    /// <summary>Detaches rows from a save that did not complete.</summary>
    /// <param name="context">The context that owns the staged rows.</param>
    private void DetachStaged(DbContext? context)
    {
        if (context is null || _stagedMessages.Count == 0)
            return;

        foreach (OutboxMessage message in _stagedMessages)
        {
            var entry = context.Entry(message);
            if (entry.State == EntityState.Added)
                entry.State = EntityState.Detached;
        }

        _stagedMessages.Clear();
        Reset();
    }

    /// <summary>Forgets the staging state for the current save.</summary>
    private void Reset()
    {
        _stagedEventIds.Clear();
        _stagedAggregates.Clear();
    }
}
