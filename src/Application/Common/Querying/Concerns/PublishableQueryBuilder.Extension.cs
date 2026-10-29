using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Lifecycle.Publishable;

namespace Application.Common.Querying.Concerns;

/// <summary>Publication-state chains for a query builder.</summary>
public static class PublishableQueryBuilderExtension
{
    /// <summary>Restricts the query to published rows.</summary>
    public static QueryBuilder<T> OnlyPublished<T>(this QueryBuilder<T> builder)
        where T : class, IPublishable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.IsPublished, true);

    /// <summary>Filters rows published after the supplied instant.</summary>
    public static QueryBuilder<T> PublishedAfter<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, IPublishable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.PublishedAtUtc,
            FilterOperator.GreaterThan,
            instant);

    /// <summary>Filters rows published before the supplied instant.</summary>
    public static QueryBuilder<T> PublishedBefore<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, IPublishable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.PublishedAtUtc,
            FilterOperator.LessThan,
            instant);

    /// <summary>Filters rows published by one actor.</summary>
    public static QueryBuilder<T> PublishedBy<T>(
        this QueryBuilder<T> builder,
        string actor)
        where T : class, IPublishable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.PublishedBy, actor);
}
