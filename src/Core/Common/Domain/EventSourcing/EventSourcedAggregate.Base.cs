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
        Result result = TryLoadFromHistory(version, history);

        if (result.IsFailure)
        {
            Error error = result.Errors[0];

            // Boundary: keep the legacy throwing API for existing callers. New flow code
            // must use TryLoadFromHistory and handle the Result.
            if (error.Code == EventSourcingOutcome.Codes.AlreadyLoaded)
                throw new InvalidOperationException(error.Message);

            if (error.Code == EventSourcingOutcome.Codes.VersionInvalid)
                throw new ArgumentOutOfRangeException(nameof(version), error.Message);

            throw new InvalidOperationException(error.Message);
        }
    }

    /// <summary>Replays history without throwing on a corrupt or repeated stream.</summary>
    /// <param name="version">The stream version of the last supplied event.</param>
    /// <param name="history">The events to replay.</param>
    /// <returns>Success with the replayed state; otherwise a stable replay failure.</returns>
    public Result TryLoadFromHistory(long version, IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (_historyLoaded || _uncommittedEvents.Count > 0)
            return Result.Fail(EventSourcingOutcome.Failure.AlreadyLoaded);

        IReadOnlyCollection<IDomainEvent> events = history as IReadOnlyCollection<IDomainEvent>
            ?? history.ToList();

        if (version < 0 || version < events.Count)
            return Result.Fail(EventSourcingOutcome.Failure.VersionInvalid(version, events.Count));

        foreach (IDomainEvent domainEvent in events)
        {
            try
            {
                When(domainEvent);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Boundary: a When failure leaves partial state; the caller must discard the aggregate.
                return Result.Fail(EventSourcingOutcome.Failure.ApplyFailed(
                    domainEvent.GetType().Name,
                    exception.Message));
            }
        }

        _historyLoaded = true;
        Version = version;
        return Result.Ok();
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