using Core.BuildingBlocks.Domain.Entities;

namespace Application.BuildingBlocks.Persistence.Repositories;

/// <summary>Defines a generic way of abstracting persistent repositories (CRUD).</summary>
public interface IPersistentRepository<TEntity, TKey> :
    IReadableRepository<TEntity, TKey>,
    ISearchableRepository<TEntity, TKey>,
    IWritableRepository<TEntity, TKey>,
    IDeletableRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
}