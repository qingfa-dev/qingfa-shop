using Application.BuildingBlocks.Querying.Sorting;

using Core.BuildingBlocks.Modeling.Concerns.Organization.Positionable;

namespace Application.BuildingBlocks.Querying.Concerns;

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
