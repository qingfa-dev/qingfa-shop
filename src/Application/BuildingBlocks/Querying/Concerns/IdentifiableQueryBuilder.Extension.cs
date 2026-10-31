using Core.BuildingBlocks.Modeling.Concerns.Foundation.Identifiable;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Identifier chains for a query builder.</summary>
public static class IdentifiableQueryBuilderExtension
{
    /// <summary>Filters rows by identifier.</summary>
    public static QueryBuilder<T> WithId<T, TKey>(
        this QueryBuilder<T> builder,
        TKey id)
        where T : class, IIdentifiable<TKey> =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.Id, id);
}
