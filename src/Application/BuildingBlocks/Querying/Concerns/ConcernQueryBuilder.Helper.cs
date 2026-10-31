using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Sorting;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Shared condition and ordering helpers for the concern query-builder extensions.</summary>
internal static class ConcernQueryBuilder
{
    /// <summary>Adds an equality condition to the normal filter.</summary>
    public static QueryBuilder<T> WhereEqual<T, TValue>(
        QueryBuilder<T> builder,
        string field,
        TValue value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Where(Condition(field, FilterOperator.Equal, value));
    }

    /// <summary>Adds an equality condition to the non-bypassable scope.</summary>
    public static QueryBuilder<T> ScopeEqual<T, TValue>(
        QueryBuilder<T> builder,
        string field,
        TValue value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ApplyScope(Condition(field, FilterOperator.Equal, value));
    }

    /// <summary>Adds a comparison condition to the normal filter.</summary>
    public static QueryBuilder<T> WhereComparison<T, TValue>(
        QueryBuilder<T> builder,
        string field,
        FilterOperator comparison,
        TValue value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Where(Condition(field, comparison, value));
    }

    /// <summary>Adds one OR group of collection-membership conditions.</summary>
    public static QueryBuilder<T> WhereAny<T, TValue>(
        QueryBuilder<T> builder,
        string field,
        IEnumerable<TValue> values)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(values);

        List<FilterNode> conditions = [.. values
            .Select(value => (FilterNode)Condition(field, FilterOperator.Contains, value))];

        return conditions.Count == 0
            ? builder
            : builder.Where(new FilterGroup(FilterLogic.Or, conditions));
    }

    /// <summary>Adds one AND group of collection-membership conditions.</summary>
    public static QueryBuilder<T> WhereAll<T, TValue>(
        QueryBuilder<T> builder,
        string field,
        IEnumerable<TValue> values)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(values);

        foreach (TValue value in values)
            builder = builder.Where(Condition(field, FilterOperator.Contains, value));

        return builder;
    }

    /// <summary>Replaces the ordering with one field.</summary>
    public static QueryBuilder<T> OrderByField<T>(
        QueryBuilder<T> builder,
        string field,
        SortDirection direction = SortDirection.Ascending)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.OrderBy(new SortField(field, direction, SortNulls.None));
    }

    /// <summary>Applies an ordering only when the query has none.</summary>
    public static QueryBuilder<T> DefaultOrderByField<T>(
        QueryBuilder<T> builder,
        string field = ConcernQueryField.CreatedAtUtc,
        SortDirection direction = SortDirection.Descending)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        Result<Query<T>> built = builder.Build();
        if (built.IsFailure)
            return builder;

        return built.Value.OrderBy.Count > 0
            ? builder
            : builder.ThenBy(new SortField(field, direction, SortNulls.None));
    }

    private static FilterCondition Condition<TValue>(
        string field,
        FilterOperator comparison,
        TValue value) =>
        new(
            field,
            comparison,
            RawValue: value?.ToString(),
            Value: value,
            ValueType: Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue),
            CaseInsensitive: false,
            IndexValue: null,
            CustomOperator: null);
}
