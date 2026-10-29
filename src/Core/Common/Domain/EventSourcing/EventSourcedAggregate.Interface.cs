using Core.Common.Domain.Events;

namespace Core.Common.Domain.EventSourcing;

/// <summary>An aggregate whose state is rebuilt from its event history.</summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public interface IEventSourcedAggregate<out TKey>
{
    /// <summary>Gets the aggregate identifier.</summary>
    TKey Id { get; }

    /// <summary>Gets the number of events applied to the aggregate.</summary>
    long Version { get; }

    /// <summary>Gets the events raised since the last successful append.</summary>
    IReadOnlyList<IDomainEvent> UncommittedEvents { get; }

    /// <summary>Replays history without marking events as uncommitted.</summary>
    void LoadFromHistory(long version, IEnumerable<IDomainEvent> history);

    /// <summary>Clears the events after a successful append.</summary>
    void ClearUncommittedEvents();
}