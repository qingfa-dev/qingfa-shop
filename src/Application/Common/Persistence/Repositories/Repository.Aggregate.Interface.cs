using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

namespace Application.Common.Persistence.Repositories;

/// <summary>Defines a generic way of abstracting persistent repositories (CRUD)
/// optimized for Domain Driven Design — adds no methods, only the aggregate constraint.</summary>
public interface IAggregateRepository<TEntity, TKey> :
    IPersistentRepository<TEntity, TKey>
    where TEntity : class, IAggregateRoot<IDomainEvent, TKey>
{
}