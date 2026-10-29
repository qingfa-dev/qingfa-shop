using Core.Common.Domain.Events;

namespace Core.Common.Domain.EventSourcing;

/// <summary>
/// Base aggregate that applies events through a single <see cref="When"/> switch.
/// </summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public abstract class EventSourcedAggregate<TKey> : IEventSourcedAggregate<TKey>
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    /// <inheritdoc />
    public TKey Id { get; protected set; } = default!;

    /// <inheritdoc />
    public long Version { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    /// <inheritdoc />
    public void LoadFromHistory(long version, IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        foreach (IDomainEvent domainEvent in history)
            When(domainEvent);

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