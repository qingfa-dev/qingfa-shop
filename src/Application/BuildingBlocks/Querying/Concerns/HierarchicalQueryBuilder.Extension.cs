using Core.BuildingBlocks.Modeling.Concerns.Organization.Hierarchical;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Hierarchy chains for a query builder.</summary>
public static class HierarchicalQueryBuilderExtension
{
    /// <summary>Filters rows by parent identifier.</summary>
    public static QueryBuilder<T> WithParent<T, TKey>(
        this QueryBuilder<T> builder,
        TKey parentId)
        where T : class, IHierarchical<TKey>
        where TKey : struct =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.ParentId, parentId);

    /// <summary>Filters rows by parent identifier; null means roots only.</summary>
    public static QueryBuilder<T> WithParent<T, TKey>(
        this QueryBuilder<T> builder,
        TKey? parentId)
        where T : class, IHierarchical<TKey>
        where TKey : struct =>
        ConcernQueryBuilder.WhereEqual(builder, ConcernQueryField.ParentId, parentId);

    /// <summary>Restricts the query to root rows.</summary>
    public static QueryBuilder<T> RootOnly<T, TKey>(this QueryBuilder<T> builder)
        where T : class, IHierarchical<TKey>
        where TKey : struct =>
        builder.WithParent<T, TKey>(null);
}
