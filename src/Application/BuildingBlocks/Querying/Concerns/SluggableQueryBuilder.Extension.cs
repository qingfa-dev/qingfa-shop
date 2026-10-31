using Application.BuildingBlocks.Querying.Filtering;

using Core.BuildingBlocks.Modeling.Concerns.Content.Sluggable;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Slug chains for a query builder.</summary>
public static class SluggableQueryBuilderExtension
{
    /// <summary>Filters rows by exact slug.</summary>
    public static QueryBuilder<T> WithSlug<T>(
        this QueryBuilder<T> builder,
        string slug)
        where T : class, ISluggable =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.Slug, slug);

    /// <summary>Filters rows whose slug contains the supplied text.</summary>
    public static QueryBuilder<T> SlugContains<T>(
        this QueryBuilder<T> builder,
        string text)
        where T : class, ISluggable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Slug,
            FilterOperator.Contains,
            text);
}
