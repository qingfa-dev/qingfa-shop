using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Lifecycle.Activatable;

namespace Application.Common.Querying.Concerns;

/// <summary>Activation-state chains for a query builder.</summary>
public static class ActivatableQueryBuilderExtension
{
    /// <summary>Restricts the query to active rows.</summary>
    public static QueryBuilder<T> OnlyActive<T>(this QueryBuilder<T> builder)
        where T : class, IActivatable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.IsActive, true);

    /// <summary>Filters rows activated after the supplied instant.</summary>
    public static QueryBuilder<T> ActivatedAfter<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, IActivatable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.ActivatedAtUtc,
            FilterOperator.GreaterThan,
            instant);

    /// <summary>Filters rows activated before the supplied instant.</summary>
    public static QueryBuilder<T> ActivatedBefore<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, IActivatable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.ActivatedAtUtc,
            FilterOperator.LessThan,
            instant);

    /// <summary>Filters rows activated by one actor.</summary>
    public static QueryBuilder<T> ActivatedBy<T>(
        this QueryBuilder<T> builder,
        string actor)
        where T : class, IActivatable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.ActivatedBy, actor);
}
