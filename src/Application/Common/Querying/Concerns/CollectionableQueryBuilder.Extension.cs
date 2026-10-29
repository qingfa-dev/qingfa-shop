using Application.Common.Querying.Filtering;

using Core.Common.Concerns.Organization.Collectionable;

namespace Application.Common.Querying.Concerns;

/// <summary>Collection-membership chains for a query builder.</summary>
public static class CollectionableQueryBuilderExtension
{
    /// <summary>Filters rows assigned to the collection.</summary>
    public static QueryBuilder<T> WithCollection<T, TCollection>(
        this QueryBuilder<T> builder,
        TCollection collection)
        where T : class, ICollectionable<TCollection> =>
        ConcernQueryBuilder.WhereComparison(
            builder,
            ConcernQueryField.Collections,
            FilterOperator.Contains,
            collection);

    /// <summary>Filters rows assigned to any supplied collection.</summary>
    public static QueryBuilder<T> WithAnyCollection<T, TCollection>(
        this QueryBuilder<T> builder,
        params TCollection[] collections)
        where T : class, ICollectionable<TCollection> =>
        ConcernQueryBuilder.WhereAny(builder, ConcernQueryField.Collections, collections);

    /// <summary>Filters rows assigned to every supplied collection.</summary>
    public static QueryBuilder<T> WithAllCollections<T, TCollection>(
        this QueryBuilder<T> builder,
        params TCollection[] collections)
        where T : class, ICollectionable<TCollection> =>
        ConcernQueryBuilder.WhereAll(builder, ConcernQueryField.Collections, collections);
}
