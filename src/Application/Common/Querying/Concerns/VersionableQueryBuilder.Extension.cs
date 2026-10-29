using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Foundation.Versionable;

namespace Application.Common.Querying.Concerns;

/// <summary>Version chains for a query builder.</summary>
public static class VersionableQueryBuilderExtension
{
    /// <summary>Filters rows at or above the supplied version.</summary>
    public static QueryBuilder<T> VersionAtLeast<T>(
        this QueryBuilder<T> builder,
        long version)
        where T : class, IVersionable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Version,
            FilterOperator.GreaterThanOrEqual,
            version);

    /// <summary>Filters rows at or below the supplied version.</summary>
    public static QueryBuilder<T> VersionAtMost<T>(
        this QueryBuilder<T> builder,
        long version)
        where T : class, IVersionable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Version,
            FilterOperator.LessThanOrEqual,
            version);
}
