using Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;

namespace Application.BuildingBlocks.Querying.Concerns;

/// <summary>Tenant scope chains for a query builder.</summary>
public static class TenantableQueryBuilderExtension
{
    /// <summary>Restricts the query to one tenant through the non-bypassable scope.</summary>
    public static QueryBuilder<T> ForTenant<T, TKey>(
        this QueryBuilder<T> builder,
        TKey tenantId)
        where T : class, ITenantable<TKey>
        where TKey : notnull =>
        ConcernQueryBuilder.ScopeEqual(builder, ConcernQueryField.TenantId, tenantId);
}
