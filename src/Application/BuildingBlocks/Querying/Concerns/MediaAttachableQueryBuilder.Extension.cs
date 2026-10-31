using Application.BuildingBlocks.Querying.Filtering;

using Core.BuildingBlocks.Modeling.Concerns.Content.MediaAttachable;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Media-membership chains for a query builder.</summary>
public static class MediaAttachableQueryBuilderExtension
{
    /// <summary>Filters rows attached to the media item.</summary>
    public static QueryBuilder<T> WithMedia<T, TMedia>(
        this QueryBuilder<T> builder,
        TMedia media)
        where T : class, IMediaAttachable<TMedia> =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Media,
            FilterOperator.Contains,
            media);

    /// <summary>Filters rows attached to any supplied media item.</summary>
    public static QueryBuilder<T> WithAnyMedia<T, TMedia>(
        this QueryBuilder<T> builder,
        params TMedia[] media)
        where T : class, IMediaAttachable<TMedia> =>
        ConcernQueryBuilder.WhereAny(builder, ConcernQueryField.Media, media);

    /// <summary>Filters rows attached to every supplied media item.</summary>
    public static QueryBuilder<T> WithAllMedia<T, TMedia>(
        this QueryBuilder<T> builder,
        params TMedia[] media)
        where T : class, IMediaAttachable<TMedia> =>
        ConcernQueryBuilder.WhereAll(builder, ConcernQueryField.Media, media);
}
