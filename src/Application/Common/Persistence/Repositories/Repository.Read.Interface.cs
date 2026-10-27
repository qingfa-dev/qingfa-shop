using Core.Common.Domain.Entities;
using Core.Common.Monad.Maybes;

namespace Application.Common.Persistence.Repositories;

/// <summary>Reads a single entity by its key (cRud).</summary>
public interface IReadableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>Returns the entity, or <see cref="RepositoryOutcome.Failure.NotFoundFor{T}"/>.</summary>
    Task<Result<TEntity>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Returns <see cref="Maybe{TEntity}.Empty()"/> when missing. Never a "not found" error.</summary>
    Task<Result<Maybe<TEntity>>> FindByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}