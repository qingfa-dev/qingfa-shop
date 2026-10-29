using System.Collections.ObjectModel;

using Core.Common.Domain.Events;

namespace Core.Common.Domain.EventSourcing;

/// <summary>
/// Base aggregate that applies events through a single <see cref="When"/> switch.
/// </summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public abstract class EventSourcedAggregate<TKey> : IEventSourcedAggregate<TKey>
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _readOnlyUncommittedEvents;
    private bool _historyLoaded;

    /// <summary>Initializes a new aggregate with no applied events.</summary>
    protected EventSourcedAggregate()
    {
        // Cache: wrap the backing list once so the public surface cannot mutate it.
        _readOnlyUncommittedEvents = _uncommittedEvents.AsReadOnly();
    }

    /// <inheritdoc />
    public TKey Id { get; protected set; } = default!;

    /// <inheritdoc />
    public long Version { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _readOnlyUncommittedEvents;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// History was already loaded, or the aggregate has uncommitted events.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The stream version is negative or below the number of supplied events.
    /// </exception>
    public void LoadFromHistory(long version, IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        // Guard: rehydration is a one-time transition from an empty aggregate to a stream state.
        if (_historyLoaded || _uncommittedEvents.Count > 0)
            throw new InvalidOperationException(
                "History can only be loaded once, before the aggregate raises new events.");

        // Materialize: enumerate the stream exactly once so counting and replay see the same events.
        IReadOnlyCollection<IDomainEvent> events = history as IReadOnlyCollection<IDomainEvent>
            ?? history.ToList();

        // Validate: the stream version counts applied events, so it cannot be below that count.
        if (version < events.Count)
            throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                "The stream version cannot be below the number of supplied events.");

        // Apply: a When failure leaves partial state; the caller must discard the aggregate.
        foreach (IDomainEvent domainEvent in events)
            When(domainEvent);

        _historyLoaded = true;
        Version = version;
    }

    /// <summary>Applies a new event and records it for the next append.</summary>
    /// <param name="domainEvent">The event to apply.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        When(domainEvent);
        Version++;
        _uncommittedEvents.Add(domainEvent);
    }

    /// <summary>Applies one event to the aggregate state.</summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <exception cref="InvalidOperationException">The event type is not handled.</exception>
    protected abstract void When(IDomainEvent domainEvent);

    /// <inheritdoc />
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();
}