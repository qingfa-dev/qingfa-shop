using Core.BuildingBlocks.Domain.Aggregates;
using Core.BuildingBlocks.Domain.Events;

namespace Application.BuildingBlocks.Persistence.Repositories;

/// <summary>Defines a generic way of abstracting persistent repositories (CRUD)
/// optimized for Domain Driven Design — adds no methods, only the aggregate constraint.</summary>
public interface IAggregateRepository<TEntity, TKey> :
    IPersistentRepository<TEntity, TKey>
    where TEntity : class, IAggregateRoot<IDomainEvent, TKey>
{
}