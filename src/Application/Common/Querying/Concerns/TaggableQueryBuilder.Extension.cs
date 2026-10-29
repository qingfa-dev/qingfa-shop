using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Content.Taggable;

namespace Application.Common.Querying.Concerns;

/// <summary>Tag-membership chains for a query builder.</summary>
public static class TaggableQueryBuilderExtension
{
    /// <summary>Filters rows containing the tag.</summary>
    public static QueryBuilder<T> WithTag<T>(this QueryBuilder<T> builder, string tag)
        where T : class, ITaggable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Tags,
            FilterOperator.Contains,
            tag);

    /// <summary>Filters rows that do not contain the tag.</summary>
    public static QueryBuilder<T> WithoutTag<T>(this QueryBuilder<T> builder, string tag)
        where T : class, ITaggable =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Tags,
            FilterOperator.NotContains,
            tag);

    /// <summary>Filters rows containing any of the supplied tags.</summary>
    public static QueryBuilder<T> WithAnyTag<T>(this QueryBuilder<T> builder, params string[] tags)
        where T : class, ITaggable =>
        ConcernQueryBuilder.WhereAny(builder, ConcernQueryField.Tags, tags);

    /// <summary>Filters rows containing every supplied tag.</summary>
    public static QueryBuilder<T> WithAllTags<T>(this QueryBuilder<T> builder, params string[] tags)
        where T : class, ITaggable =>
        ConcernQueryBuilder.WhereAll(builder, ConcernQueryField.Tags, tags);
}
