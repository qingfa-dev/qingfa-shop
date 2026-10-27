using Core.Common.Domain.Entities;

using Application.Common.Querying;
using Application.Common.Querying.Pagination;

namespace Application.Common.Persistence.Repositories;

/// <summary>Paged / scalar queries over an entity set (cRud).</summary>
public interface ISearchableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<Result<QueryPage<TEntity>>> QueryAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default);

    /// <summary>Fails with <see cref="RepositoryOutcome.Failure.ShapeMismatch"/> if not exactly one row.</summary>
    Task<Result<TEntity>> QuerySingleAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default);

    /// <summary>Returns <see cref="Maybe{TEntity}.Empty()"/> when no row matches. Never a "not found" error.</summary>
    Task<Result<Maybe<TEntity>>> QueryFirstOrDefaultAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default);

    Task<Result<int>> CountAsync(Query<TEntity> query, CancellationToken cancellationToken = default);
    Task<Result<bool>> AnyAsync(Query<TEntity> query, CancellationToken cancellationToken = default);
}