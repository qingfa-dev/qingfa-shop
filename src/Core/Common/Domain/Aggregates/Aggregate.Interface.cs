using Core.Common.Domain.Events;

namespace Core.Common.Domain.Aggregates;

/// <summary>
/// Defines a marker interface of an Aggregate as specified in Domain Driven Design.
/// </summary>
public interface IAggregate : IHasMetadata
{
}

/// <summary>
/// Defines an Event based contract of an Aggregate as specified in Domain Driven Design.
/// </summary>
/// <typeparam name="TEvent">The type of the event that implements the <see cref="IDomainEvent"/> interface.</typeparam>
/// <seealso cref="IAggregate" />
public interface IAggregate<out TEvent> : IAggregate
{
    /// <summary>
    /// Gets the events that was added to the Aggregate.
    /// </summary>
    /// <value>The events added to the Aggregate.</value>
    IReadOnlyList<TEvent> Events { get; }

    /// <summary>
    /// Removes all events from the Aggregate.
    /// </summary>
    void RemoveAllEvents();
}