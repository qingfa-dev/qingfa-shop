using Application.Common.Persistence.Repositories;

using Core.Common.Domain.Entities;

namespace Application.Common.Persistence;

/// <summary>Reads a single entity by its key (cRud).</summary>
public interface IReadableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>Returns the entity, or <see cref="PersistenceOutcome.Failure.NotFoundFor{T}"/>.</summary>
    Task<Result<TEntity>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Returns <c>null</c> when missing. Never a "not found" error.</summary>
    Task<Result<TEntity?>> FindByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}