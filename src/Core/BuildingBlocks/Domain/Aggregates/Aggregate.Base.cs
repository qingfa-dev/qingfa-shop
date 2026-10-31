using Core.BuildingBlocks.Domain.Entities;

using Core.BuildingBlocks.Domain.Events;

namespace Core.BuildingBlocks.Domain.Aggregates;

/// <summary>
/// Represents the base class from which all implementations of an Aggregate Root (as specified in Domain Driven Design) should derive.
/// </summary>
/// <typeparam name="TKey">The type of the key that uniquely identifies this aggregate.</typeparam>
/// <typeparam name="TEvent">The type of the event that implements the <see cref="IDomainEvent"/> interface.</typeparam>
/// <seealso cref="Entity{TKey}" />
public abstract class Aggregate<TKey, TEvent> : Entity<TKey>, IAggregate<TEvent>
{
    private readonly List<TEvent> _events = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Aggregate{TKey, TEvent}"/> class.
    /// </summary>
    /// <param name="metadata">The optional metadata to merge with this instance.</param>
    protected Aggregate(IMetadata? metadata = null)
    {
        if (metadata is not null)
            this.MergeMetadata(metadata);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Aggregate{TKey, TEvent}"/> class.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    protected Aggregate(TKey id) : base(id)
    {
    }

    /// <summary>
    /// Gets the events that was added to the Aggregate.
    /// </summary>
    /// <value>The events added to the Aggregate.</value>
    public IReadOnlyList<TEvent> Events => _events;

    /// <summary>
    /// Gets the associated metadata of this Aggregate.
    /// </summary>
    /// <value>The associated metadata of this Aggregate.</value>
    public DefaultMetadata Metadata { get; } = new DefaultMetadata();

    /// <summary>
    /// Gets or sets the optimistic concurrency version. New aggregates start at zero.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Adds an event to the Aggregate.
    /// </summary>
    /// <param name="e">The event to be added to the end of <see cref="Events"/>.</param>
    protected virtual void AddEvent(TEvent e)
    {
        _events.Add(e);
    }

    /// <summary>
    /// Removes all events from the Aggregate.
    /// </summary>
    public void RemoveAllEvents()
    {
        _events.Clear();
    }
}