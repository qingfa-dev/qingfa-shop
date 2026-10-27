using Application.Common.Persistence;
using Application.Common.Persistence.Capabilities;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;

using Core.Common.Domain.Entities;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>Raw projection reader (cRud-tabular).</summary>
public partial class EfCoreRepository<TEntity, TKey> : IProjectionRepository<TEntity, TKey>
{
    /// <inheritdoc />
    public async Task<Result<TabularResult>> QueryTabularAsync(
        Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate: projection must be supported by the provider
            var capResult = QueryCapabilityGuard.ValidateCapabilitySupported(
                "Projection", Capabilities.SupportsProjection);
            if (capResult.IsFailure)
                return Result<TabularResult>.Fail(capResult.Errors);

            // Validate: grouping must be supported if query uses group-by
            if (query.GroupBy is not null && query.GroupBy.Count > 0)
            {
                var groupResult = QueryCapabilityGuard.ValidateCapabilitySupported(
                    "Grouping", Capabilities.SupportsGrouping);
                if (groupResult.IsFailure)
                    return Result<TabularResult>.Fail(groupResult.Errors);
            }

            // Validate: aggregates must be supported if query uses aggregates
            if (query.Aggregate is not null && query.Aggregate.Count > 0)
            {
                var aggResult = QueryCapabilityGuard.ValidateCapabilitySupported(
                    "Aggregates", Capabilities.SupportsAggregates);
                if (aggResult.IsFailure)
                    return Result<TabularResult>.Fail(aggResult.Errors);
            }

            Result<IQueryable<TEntity>> buildResult = BuildQuery(query);
            if (buildResult.IsFailure)
                return Result<TabularResult>.Fail(buildResult.Errors);

            IQueryable<TEntity> queryable = buildResult.Value;

            // For select-only queries, use server-side projection
            if (query.Shape == QueryShape.Tabular && query.Aggregate is null && query.GroupBy is null)
            {
                if (query.Select is null || query.Select.Count == 0)
                    return Result<TabularResult>.Fail(RepositoryOutcome.Failure.QueryInvalid("No columns selected"));

                Result<TabularPlan> plan = _translator.BuildSelectPlan(queryable, query.Select, _map);
                if (plan.IsFailure)
                    return Result<TabularResult>.Fail(plan.Errors);

                // Apply paging if requested
                IQueryable<object[]> rowsQuery = plan.Value.Rows;
                if (query.Page is not null)
                {
                    var pagedResult = ApplyPagingToTabular(rowsQuery, query.Page);
                    if (pagedResult.IsFailure)
                        return Result<TabularResult>.Fail(pagedResult.Errors);

                    rowsQuery = pagedResult.Value;
                }

                var rowObjects = await rowsQuery.ToListAsync(cancellationToken);
                var tabularRows = rowObjects.Select(r => new TabularRow((IReadOnlyList<object?>)r)).ToList();

                return Result<TabularResult>.Success(new TabularResult(plan.Value.Columns, tabularRows));
            }

            // For group/aggregate queries, materialize and evaluate client-side
            var entityRows = await queryable.ToListAsync(cancellationToken);
            Result<TabularResult> result = _translator.BuildTabularClientSide(entityRows, query, _map);

            return result;
        }
        catch (Exception ex)
        {
            return Result<TabularResult>.Fail(TranslateException(ex));
        }
    }

    private static Result<IQueryable<object[]>> ApplyPagingToTabular(
        IQueryable<object[]> source, PageRequest page)
    {
        try
        {
            int skip = (page.Page - 1) * page.PageSize;
            var paged = source.Skip(skip).Take(page.PageSize);
            return Result<IQueryable<object[]>>.Success(paged);
        }
        catch
        {
            return Result<IQueryable<object[]>>.Fail(RepositoryOutcome.Failure.QueryInvalid("Invalid page parameters"));
        }
    }
}
