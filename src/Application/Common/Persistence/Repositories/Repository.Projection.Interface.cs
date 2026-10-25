using Core.Common.Domain.Entities;

using Application.Common.Querying;
using Application.Common.Querying.Projection;

namespace Application.Common.Persistence.Repositories;

/// <summary>Raw projection reader (cRud-tabular). Primary home for Dapper;
/// EF Core satisfies it via <c>Select</c> or <c>FromSqlRaw</c>.</summary>
public interface IProjectionRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<Result<TabularResult>> QueryTabularAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default);
}

/// <summary>Streaming reader. Dapper: <c>ExecuteReaderAsync</c>; EF Core: <c>AsAsyncEnumerable</c>.
/// Guard with <see cref="IQueryCapabilities.SupportsStreaming"/>.</summary>
public interface IStreamableRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>Returns <see cref="Result{T}"/> because the cursor opens before iteration
    /// can fail — a plain <c>IAsyncEnumerable</c> would throw mid-iteration.</summary>
    Task<Result<IAsyncEnumerable<TEntity>>> StreamAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default);
}