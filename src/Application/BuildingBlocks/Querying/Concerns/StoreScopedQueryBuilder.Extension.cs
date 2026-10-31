using Core.BuildingBlocks.Modeling.Concerns.Foundation.StoreScoped;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Store scope chains for a query builder.</summary>
public static class StoreScopedQueryBuilderExtension
{
    /// <summary>Restricts the query to one store.</summary>
    public static QueryBuilder<T> ForStore<T, TKey>(
        this QueryBuilder<T> builder,
        TKey storeId)
        where T : class, IStoreScoped<TKey>
        where TKey : notnull =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.StoreId, storeId);
}
