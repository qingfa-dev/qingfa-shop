using Application.Common.Querying;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Sorting;

namespace Application.Features.Todos.Common;

/// <summary>Builds the tenant and soft-delete scope every Todo search shares.</summary>
internal static class TodoQueryScope
{
    /// <summary>The order applied when the caller does not request one.</summary>
    public const string DefaultSortField = "createdAtUtc";

    /// <summary>
    /// Narrows a parsed query to the tenant, hides soft-deleted rows unless asked,
    /// and supplies the default order.
    /// </summary>
    /// <typeparam name="TEntity">The entity the query targets.</typeparam>
    /// <param name="query">The parsed query.</param>
    /// <param name="tenantId">The tenant that owns the rows.</param>
    /// <param name="includeDeleted">Whether soft-deleted rows are visible.</param>
    /// <returns>The scoped query.</returns>
    public static Query<TEntity> Apply<TEntity>(
        Query<TEntity> query,
        Guid tenantId,
        bool includeDeleted)
    {
        ArgumentNullException.ThrowIfNull(query);

        FilterNode scope = BuildScope(tenantId, includeDeleted);
        FilterNode filter = query.Filter is null
            ? scope
            : new FilterGroup(FilterLogic.And, [scope, query.Filter]);

        IReadOnlyList<SortField> order = query.OrderBy.Count > 0
            ? query.OrderBy
            : [new SortField(DefaultSortField, SortDirection.Descending, SortNulls.None)];

        return query with { Filter = filter, OrderBy = order };
    }

    private static FilterNode BuildScope(Guid tenantId, bool includeDeleted)
    {
        List<FilterNode> predicates =
        [
            new FilterCondition(
                "tenantId",
                FilterOperator.Equal,
                RawValue: null,
                Value: tenantId,
                ValueType: typeof(Guid),
                CaseInsensitive: false,
                IndexValue: null,
                CustomOperator: null),
        ];

        if (!includeDeleted)
        {
            predicates.Add(new FilterCondition(
                "isDeleted",
                FilterOperator.Equal,
                RawValue: null,
                Value: false,
                ValueType: typeof(bool),
                CaseInsensitive: false,
                IndexValue: null,
                CustomOperator: null));
        }

        return new FilterGroup(FilterLogic.And, predicates);
    }
}
