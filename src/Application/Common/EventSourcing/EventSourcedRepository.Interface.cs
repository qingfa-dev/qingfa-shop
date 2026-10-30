using Core.Common.Domain.EventSourcing;

namespace Application.Common.EventSourcing;

/// <summary>Loads and saves an event-sourced aggregate.</summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
/// <typeparam name="TKey">The aggregate id type.</typeparam>
public interface IEventSourcedRepository<TAggregate, in TKey>
    where TAggregate : class, IEventSourcedAggregate<TKey>
{
    /// <summary>Loads the aggregate from its snapshot and stream tail.</summary>
    Task<Result<TAggregate>> LoadAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Appends uncommitted events and stages their outbox rows.</summary>
    Task<Result> SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
