using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>Paged / scalar queries over an entity set (cRud).</summary>
public partial class EfCoreRepository<TEntity, TKey> : ISearchableRepository<TEntity, TKey>
{
    /// <inheritdoc />
    /// <remarks>
    /// Count: LongCountAsync is called before paging so the total count reflects the full
    /// filtered set, not the paged subset. The count query is cheap (COUNT(*) without LIMIT)
    /// and must precede the paged materialization.
    /// </remarks>
    public async Task<Result<QueryPage<TEntity>>> QueryAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate: page size against provider limit.
            if (query.Page is not null && Capabilities.MaxPageSize is int maxPageSize)
            {
                var pageResult = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(
                    query.Page.PageSize, maxPageSize);
                if (pageResult.IsFailure)
                    return Result<QueryPage<TEntity>>.Fail(pageResult.Errors);
            }

            // Validate: order-by clause count against provider limit.
            if (Capabilities.MaxOrderByClauses is int maxOrderBy && query.OrderBy.Count > maxOrderBy)
            {
                return Result<QueryPage<TEntity>>.Fail(
                    QueryCapabilityOutcome.Failure.OrderByClausesExceedProviderLimit(
                        query.OrderBy.Count, maxOrderBy));
            }

            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<QueryPage<TEntity>>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;

            // Fetch total count before paging.
            long totalCount = await queryable.LongCountAsync(cancellationToken);

            // Apply paging if requested.
            if (query.Page is not null)
            {
                Result<IQueryable<TEntity>> paged = _translator.ApplyPaging(queryable, query.Page);
                if (paged.IsFailure)
                    return Result<QueryPage<TEntity>>.Fail(paged.Errors);

                queryable = paged.Value;
            }

            // Materialize the page.
            var items = await queryable.ToListAsync(cancellationToken);

            var page = new QueryPage<TEntity>(
                items,
                query.Page?.Page ?? 1,
                query.Page?.PageSize ?? items.Count,
                totalCount);

            return Result<QueryPage<TEntity>>.Success(page);
        }
        catch (Exception ex)
        {
            // Translate: query execution failure to typed persistence outcome.
            return Result<QueryPage<TEntity>>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TEntity>> QuerySingleAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<TEntity>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;
            var entity = await queryable.SingleOrDefaultAsync(cancellationToken);

            if (entity is null)
                return Result<TEntity>.Fail(RepositoryOutcome.Failure.NotFound);

            return Result<TEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            // Translate: query execution failure to typed persistence outcome.
            return Result<TEntity>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Maybe<TEntity>>> QueryFirstOrDefaultAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<Maybe<TEntity>>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;
            var entity = await queryable.FirstOrDefaultAsync(cancellationToken);

            Maybe<TEntity> maybe = entity is not null
                ? Maybe<TEntity>.From(entity)
                : Maybe<TEntity>.Empty();

            return Result<Maybe<TEntity>>.Success(maybe);
        }
        catch (Exception ex)
        {
            // Translate: query execution failure to typed persistence outcome.
            return Result<Maybe<TEntity>>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> CountAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<int>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;
            int count = await queryable.CountAsync(cancellationToken);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            // Translate: query execution failure to typed persistence outcome.
            return Result<int>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> AnyAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<bool>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;
            bool any = await queryable.AnyAsync(cancellationToken);

            return Result<bool>.Success(any);
        }
        catch (Exception ex)
        {
            // Translate: query execution failure to typed persistence outcome.
            return Result<bool>.Fail(TranslateException(ex));
        }
    }

    #region Private Helpers

    /// <summary>
    /// Builds a queryable from a Query by applying filter, search, and sort.
    /// Returns a failure Result if any translation step fails, maintaining the Result pattern.
    /// </summary>
    /// <remarks>
    /// Read: AsNoTracking is the default for search queries — they are read-only and should not
    /// incur the overhead of EF Core change tracking (snapshot creation, state management).
    /// </remarks>
    private Result<IQueryable<TEntity>> BuildQuery(Query<TEntity> query)
    {
        IQueryable<TEntity> source = _context.Set<TEntity>().AsNoTracking();

        // Apply filter.
        if (query.Filter is not null)
        {
            Result<IQueryable<TEntity>> filtered = _translator.ApplyFilter(source, query.Filter, _map);
            if (filtered.IsFailure)
                return Result<IQueryable<TEntity>>.Fail(filtered.Errors);

            source = filtered.Value;
        }

        // Apply search.
        if (query.Search is not null)
        {
            Result<IQueryable<TEntity>> searched = _translator.ApplySearch(
                source, query.Search.Query, _map);
            if (searched.IsFailure)
                return Result<IQueryable<TEntity>>.Fail(searched.Errors);

            source = searched.Value;
        }

        // Apply sort.
        if (query.OrderBy is not null && query.OrderBy.Count > 0)
        {
            Result<IQueryable<TEntity>> sorted = _translator.ApplySort(
                source, query.OrderBy, _map);
            if (sorted.IsFailure)
                return Result<IQueryable<TEntity>>.Fail(sorted.Errors);

            source = sorted.Value;
        }

        return Result<IQueryable<TEntity>>.Success(source);
    }

    #endregion
}
