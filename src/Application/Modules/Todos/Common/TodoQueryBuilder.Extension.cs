using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Concerns;
using Application.BuildingBlocks.Querying.Sorting;

using Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;

namespace Application.Modules.Todos.Common;

/// <summary>Narrows a Todo query to the tenant, soft-delete, and default-order scope.</summary>
public static class TodoQueryBuilderExtension
{
    /// <summary>Adds the tenant equality condition.</summary>
    /// <typeparam name="T">The tenantable entity the query addresses.</typeparam>
    /// <param name="builder">The builder to scope.</param>
    /// <param name="tenantId">The tenant that owns the rows.</param>
    /// <returns>A new builder whose state carries the tenant condition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static QueryBuilder<T> ForTenant<T>(
        this QueryBuilder<T> builder,
        Guid tenantId)
        where T : class, ITenantable<Guid>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return TenantableQueryBuilderExtension.ForTenant(builder, tenantId);
    }

    /// <summary>Hides soft-deleted rows unless they are explicitly included.</summary>
    /// <typeparam name="T">The soft-deletable entity the query addresses.</typeparam>
    /// <param name="builder">The builder to scope.</param>
    /// <param name="includeDeleted">True to leave the deletion scope unapplied.</param>
    /// <returns>A new builder whose state carries the deletion condition when needed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static QueryBuilder<T> IncludeDeleted<T>(
        this QueryBuilder<T> builder,
        bool includeDeleted = true)
        where T : class, ISoftDeletable
    {
        ArgumentNullException.ThrowIfNull(builder);
        return SoftDeletableQueryBuilderExtension.IncludeDeleted(builder, includeDeleted);
    }

    /// <summary>Applies the default order when the caller supplied no ordering.</summary>
    /// <typeparam name="T">The entity the query addresses.</typeparam>
    /// <param name="builder">The builder to order.</param>
    /// <param name="field">The order field; defaults to the creation instant.</param>
    /// <param name="direction">The order direction; defaults to descending.</param>
    /// <returns>The builder unchanged when an order exists or the state has failed; otherwise the default order is appended.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static QueryBuilder<T> DefaultOrderBy<T>(
        this QueryBuilder<T> builder,
        string field = "createdAtUtc",
        SortDirection direction = SortDirection.Descending)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return ConcernQueryBuilder.DefaultOrderByField(builder, field, direction);
    }

    /// <summary>Applies the tenant, deletion, and default-order scope every Todo search shares.</summary>
    /// <typeparam name="T">The tenantable and soft-deletable entity the query addresses.</typeparam>
    /// <param name="builder">The builder to scope.</param>
    /// <param name="tenantId">The tenant that owns the rows.</param>
    /// <param name="includeDeleted">True to leave soft-deleted rows visible.</param>
    /// <returns>A new builder whose state carries the combined scope.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static QueryBuilder<T> ApplyTodoScope<T>(
        this QueryBuilder<T> builder,
        Guid tenantId,
        bool includeDeleted)
        where T : class, ITenantable<Guid>, ISoftDeletable =>
        builder
            .ForTenant(tenantId)
            .IncludeDeleted(includeDeleted)
            .DefaultOrderBy();
}