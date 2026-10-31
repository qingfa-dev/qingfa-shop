using Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.BuildingBlocks.Domain.Entities;

namespace Application.BuildingBlocks.Persistence.Repositories;

/// <summary>Tenant-scoped lookup extensions shared by application use cases.</summary>
public static class RepositoryOwnershipExtension
{
    /// <summary>
    /// Finds an entity by key and fails with not-found unless it belongs to the tenant.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository to read from.</param>
    /// <param name="id">The identifier to look up.</param>
    /// <param name="tenantId">The tenant that must own the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="includeDeleted">Whether soft-deleted rows are visible.</param>
    /// <returns>The owned entity, or a not-found failure.</returns>
    public static async Task<Result<TEntity>> FindOwnedAsync<TEntity>(
        this IPersistentRepository<TEntity, Guid> repository,
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken,
        bool includeDeleted = false)
        where TEntity : class, IEntity<Guid>, ITenantable<Guid>, ISoftDeletable
    {
        Result<Maybe<TEntity>> found = await repository
            .FindByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        return found
            .Bind(maybe => maybe.HasValue
                ? Result<TEntity>.Ok(maybe.Value)
                : Result<TEntity>.Fail(RepositoryOutcome.Failure.NotFoundFor<TEntity>(id)))
            .Ensure(
                entity => entity.TenantId == tenantId && (includeDeleted || !entity.IsDeleted),
                RepositoryOutcome.Failure.NotFoundFor<TEntity>(id));
    }
}
