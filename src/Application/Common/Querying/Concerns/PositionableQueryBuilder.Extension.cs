using Application.Common.Querying.Sorting;

using Core.Common.Concerns.Organization.Positionable;

namespace Application.Common.Querying.Concerns;

/// <summary>Position ordering chains for a query builder.</summary>
public static class PositionableQueryBuilderExtension
{
    /// <summary>Orders rows by position ascending.</summary>
    public static QueryBuilder<T> OrderByPosition<T>(
        this QueryBuilder<T> builder,
        SortDirection direction = SortDirection.Ascending)
        where T : class, IPositionable =>
        ConcernQueryBuilder.OrderByField(builder, ConcernQueryField.Position, direction);
}
