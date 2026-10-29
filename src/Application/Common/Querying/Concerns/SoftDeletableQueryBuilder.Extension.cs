using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Lifecycle.SoftDeletable;

namespace Application.Common.Querying.Concerns;

/// <summary>Soft-delete chains for a query builder.</summary>
public static class SoftDeletableQueryBuilderExtension
{
    /// <summary>Hides soft-deleted rows unless they are explicitly included.</summary>
    public static QueryBuilder<T> IncludeDeleted<T>(
        this QueryBuilder<T> builder,
        bool includeDeleted = true)
        where T : class, ISoftDeletable =>
        includeDeleted
            ? builder
            : ConcernQueryBuilder.ScopeEqual(builder, ConcernQueryField.IsDeleted, false);

    /// <summary>Restricts the query to soft-deleted rows.</summary>
    public static QueryBuilder<T> OnlyDeleted<T>(this QueryBuilder<T> builder)
        where T : class, ISoftDeletable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.IsDeleted, true);

    /// <summary>Filters rows deleted after the supplied instant.</summary>
    public static QueryBuilder<T> DeletedAfter<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, ISoftDeletable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.DeletedAtUtc,
            FilterOperator.GreaterThan,
            instant);

    /// <summary>Filters rows deleted before the supplied instant.</summary>
    public static QueryBuilder<T> DeletedBefore<T>(
        this QueryBuilder<T> builder,
        DateTimeOffset instant)
        where T : class, ISoftDeletable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.DeletedAtUtc,
            FilterOperator.LessThan,
            instant);
}
