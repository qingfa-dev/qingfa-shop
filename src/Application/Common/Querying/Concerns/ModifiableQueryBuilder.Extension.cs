using Application.Common.Querying.Filtering;
using Application.Common.Querying.Sorting;

using Core.Common.Concerns.Lifecycle.Modifiable;

namespace Application.Common.Querying.Concerns;

/// <summary>Modification-audit chains for a query builder.</summary>
public static class ModifiableQueryBuilderExtension
{
    /// <summary>Filters rows modified after the supplied instant.</summary>
    public static QueryBuilder<T> ModifiedAfter<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, IModifiable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.ModifiedAtUtc,
            FilterOperator.GreaterThan,
            instant);

    /// <summary>Filters rows modified before the supplied instant.</summary>
    public static QueryBuilder<T> ModifiedBefore<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, IModifiable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.ModifiedAtUtc,
            FilterOperator.LessThan,
            instant);

    /// <summary>Filters rows last modified by one actor.</summary>
    public static QueryBuilder<T> ModifiedBy<T>(
        this QueryBuilder<T> builder,
        string actor)
        where T : class, IModifiable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.ModifiedBy, actor);

    /// <summary>Orders rows by modification instant.</summary>
    public static QueryBuilder<T> OrderByModified<T>(
        this QueryBuilder<T> builder,
        SortDirection direction = SortDirection.Descending)
        where T : class, IModifiable =>
        ConcernQueryBuilder.OrderByField(builder, ConcernQueryField.ModifiedAtUtc, direction);
}
