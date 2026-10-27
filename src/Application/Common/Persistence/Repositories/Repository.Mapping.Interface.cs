using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Pagination;

using Core.Common.Domain.Entities;

namespace Application.Common.Persistence.Repositories;

/// <summary>
/// Maps entities to DTOs after materialization or projects at the database level.
/// Returns a <see cref="QueryPage{TDto}"/> with pagination metadata (Items, Page, PageSize, TotalCount).
/// </summary>
public interface IMappableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// Loads entities matching the query, then maps each entity to <typeparamref name="TDto"/>
    /// using the provided <paramref name="mapper"/> delegate.
    /// Entities are fully materialized before mapping, so all columns are queried.
    /// Returns a <see cref="QueryPage{TDto}"/> with total count and pagination metadata.
    /// </summary>
    Task<Result<QueryPage<TDto>>> MapAsync<TDto>(
        Query<TEntity> query,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects entities matching the query directly to <typeparamref name="TDto"/>
    /// using the provided <paramref name="projection"/> expression before materialization.
    /// Only the columns referenced in the projection are queried, making this more efficient.
    /// Returns a <see cref="QueryPage{TDto}"/> with total count and pagination metadata.
    /// </summary>
    Task<Result<QueryPage<TDto>>> ProjectAsync<TDto>(
        Query<TEntity> query,
        Expression<Func<TEntity, TDto>> projection,
        CancellationToken cancellationToken = default);
}
