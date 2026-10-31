using System.Linq.Expressions;

using Application.BuildingBlocks.Persistence.Capabilities;
using Application.BuildingBlocks.Persistence.Repositories;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Repositories;

/// <summary>
/// DTO mapping via post-materialization mapping or server-side projection.
/// </summary>
public partial class EfCoreRepository<TEntity, TKey> : IMappableRepository<TEntity, TKey>
{
    /// <inheritdoc />
    /// <remarks>
    /// Map: entities are materialized first, then mapped to DTOs using the provided delegate.
    /// This is client-side mapping after EF Core materialization, suitable for complex mappings
    /// that cannot be expressed as a simple expression tree.
    /// </remarks>
    public async Task<Result<QueryPage<TDto>>> MapAsync<TDto>(
        Query<TEntity> query,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate: page size against provider limit
            if (query.Page is not null && Capabilities.MaxPageSize is int maxPageSize)
            {
                var pageResult = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(
                    query.Page.PageSize, maxPageSize);
                if (pageResult.IsFailure)
                    return Result<QueryPage<TDto>>.Fail(pageResult.Errors);
            }

            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<QueryPage<TDto>>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;

            // Fetch total count before paging
            long totalCount = await queryable.LongCountAsync(cancellationToken);

            // Apply paging if requested
            if (query.Page is not null)
            {
                Result<IQueryable<TEntity>> paged = _translator.ApplyPaging(queryable, query.Page);
                if (paged.IsFailure)
                    return Result<QueryPage<TDto>>.Fail(paged.Errors);

                queryable = paged.Value;
            }

            // Materialize entities
            var entities = await queryable.ToListAsync(cancellationToken);

            // Map entities to DTOs
            var dtos = entities.Select(mapper).ToList().AsReadOnly();

            var page = new QueryPage<TDto>(
                dtos,
                query.Page?.Page ?? 1,
                query.Page?.PageSize ?? dtos.Count,
                totalCount);

            return Result<QueryPage<TDto>>.Success(page);
        }
        catch (Exception ex)
        {
            return Result<QueryPage<TDto>>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Project: the Expression&lt;Func&lt;TEntity, TDto&gt;&gt; enables server-side projection —
    /// EF Core translates the expression tree into a SQL SELECT clause, so only the projected
    /// columns are retrieved from the database, reducing network and memory overhead.
    /// </remarks>
    public async Task<Result<QueryPage<TDto>>> ProjectAsync<TDto>(
        Query<TEntity> query,
        Expression<Func<TEntity, TDto>> projection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate: projection must be supported by the provider
            var capResult = QueryCapabilityGuard.ValidateCapabilitySupported(
                "Projection", Capabilities.SupportsProjection);
            if (capResult.IsFailure)
                return Result<QueryPage<TDto>>.Fail(capResult.Errors);

            // Validate: page size against provider limit
            if (query.Page is not null && Capabilities.MaxPageSize is int maxPageSize)
            {
                var pageResult = QueryCapabilityGuard.ValidatePageSizeWithinProviderLimit(
                    query.Page.PageSize, maxPageSize);
                if (pageResult.IsFailure)
                    return Result<QueryPage<TDto>>.Fail(pageResult.Errors);
            }

            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<QueryPage<TDto>>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;

            // Fetch total count before paging
            long totalCount = await queryable.LongCountAsync(cancellationToken);

            // Apply paging if requested
            if (query.Page is not null)
            {
                Result<IQueryable<TEntity>> paged = _translator.ApplyPaging(queryable, query.Page);
                if (paged.IsFailure)
                    return Result<QueryPage<TDto>>.Fail(paged.Errors);

                queryable = paged.Value;
            }

            // Apply server-side projection
            IQueryable<TDto> projected = queryable.Select(projection);

            // Materialize projected DTOs
            var dtos = await projected.ToListAsync(cancellationToken);

            var page = new QueryPage<TDto>(
                dtos.AsReadOnly(),
                query.Page?.Page ?? 1,
                query.Page?.PageSize ?? dtos.Count,
                totalCount);

            return Result<QueryPage<TDto>>.Success(page);
        }
        catch (Exception ex)
        {
            return Result<QueryPage<TDto>>.Fail(TranslateException(ex));
        }
    }
}
