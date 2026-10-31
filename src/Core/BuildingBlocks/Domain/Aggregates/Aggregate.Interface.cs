using Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;
using Core.BuildingBlocks.Domain.Events;

namespace Core.BuildingBlocks.Domain.Aggregates;

/// <summary>
/// Defines a marker interface of an Aggregate as specified in Domain Driven Design.
/// </summary>
public interface IAggregate : IHasMetadata, IVersionable
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
    IReadOnlyList<TEvent> Events { get; }

    /// <summary>
    /// Removes all events from the Aggregate.
    /// </summary>
    void RemoveAllEvents();
}
