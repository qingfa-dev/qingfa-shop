using Application.BuildingBlocks.Querying.Filtering;

using Core.BuildingBlocks.Modeling.Concerns.Content.Metafieldable;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Metafield-membership chains for a query builder.</summary>
public static class MetafieldableQueryBuilderExtension
{
    /// <summary>Filters rows carrying the metafield.</summary>
    public static QueryBuilder<T> WithMetafield<T>(
        this QueryBuilder<T> builder,
        Metafield metafield)
        where T : class, IMetafieldable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Metafields,
            FilterOperator.Contains,
            metafield);

    /// <summary>Filters rows carrying any supplied metafield.</summary>
    public static QueryBuilder<T> WithAnyMetafield<T>(
        this QueryBuilder<T> builder,
        params Metafield[] metafields)
        where T : class, IMetafieldable =>
        ConcernQueryBuilder.WhereAny(builder, ConcernQueryField.Metafields, metafields);

    /// <summary>Filters rows carrying every supplied metafield.</summary>
    public static QueryBuilder<T> WithAllMetafields<T>(
        this QueryBuilder<T> builder,
        params Metafield[] metafields)
        where T : class, IMetafieldable =>
        ConcernQueryBuilder.WhereAll(builder, ConcernQueryField.Metafields, metafields);
}
