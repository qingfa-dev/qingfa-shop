using Core.Common.Domain.Entities;

using Core.Common.Domain.Events;

namespace Core.Common.Domain.Aggregates;


/// <summary>
/// Defines an Event and Entity based contract of an Aggregate as specified in Domain Driven Design.
/// </summary>
/// <typeparam name="TEvent">The type of the event that implements the <see cref="IDomainEvent"/> interface.</typeparam>
/// <typeparam name="TKey">The type of the key that uniquely identifies this Aggregate.</typeparam>
/// <seealso cref="IAggregate{TEvent}" />
/// <seealso cref="IEntity{TKey}" />
public interface IAggregateRoot<out TEvent, out TKey> : IAggregate<TEvent>, IEntity<TKey>
{
}