using Application.Common.Querying.Filtering;
using Application.Common.Querying.Sorting;

using Core.Common.Concerns.Lifecycle.Creatable;

namespace Application.Common.Querying.Concerns;

/// <summary>Creation-audit chains for a query builder.</summary>
public static class CreatableQueryBuilderExtension
{
    /// <summary>Filters rows created after the supplied instant.</summary>
    public static QueryBuilder<T> CreatedAfter<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, ICreatable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.CreatedAtUtc,
            FilterOperator.GreaterThan,
            instant);

    /// <summary>Filters rows created before the supplied instant.</summary>
    public static QueryBuilder<T> CreatedBefore<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, ICreatable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.CreatedAtUtc,
            FilterOperator.LessThan,
            instant);

    /// <summary>Filters rows created by one actor.</summary>
    public static QueryBuilder<T> CreatedBy<T>(
        this QueryBuilder<T> builder,
        string actor)
        where T : class, ICreatable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.CreatedBy, actor);

    /// <summary>Orders rows by creation instant.</summary>
    public static QueryBuilder<T> OrderByCreated<T>(
        this QueryBuilder<T> builder,
        SortDirection direction = SortDirection.Descending)
        where T : class, ICreatable =>
        ConcernQueryBuilder.OrderByField(builder, ConcernQueryField.CreatedAtUtc, direction);

    /// <summary>Applies the creation ordering only when the caller supplied none.</summary>
    public static QueryBuilder<T> DefaultOrderByCreated<T>(
        this QueryBuilder<T> builder,
        SortDirection direction = SortDirection.Descending)
        where T : class, ICreatable =>
        ConcernQueryBuilder.DefaultOrderByField(builder, ConcernQueryField.CreatedAtUtc, direction);
}
