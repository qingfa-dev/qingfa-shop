using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Domain.Entities;

namespace Application.Common.Persistence.Repositories;

/// <summary>Persistence extensions that keep a write on the success track.</summary>
public static class RepositoryPersistExtension
{
    /// <summary>Stages a new entity and commits it.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository to add to.</param>
    /// <param name="unitOfWork">The unit of work that commits.</param>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted entity, or the first failure in the chain.</returns>
    public static Task<Result<TEntity>> SaveNewAsync<TEntity>(
        this IPersistentRepository<TEntity, Guid> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity<Guid>
        => Result<TEntity>.Ok(entity)
            .ThenAsync((current, ct) => repository.AddAsync(current, ct), cancellationToken)
            .ThenAsync((current, ct) => unitOfWork.SaveChangesAsync(ct), cancellationToken);

    /// <summary>Stamps the modification audit, stages the update, and commits it.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository to update.</param>
    /// <param name="unitOfWork">The unit of work that commits.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="nowUtc">The modification instant.</param>
    /// <param name="actor">The actor performing the change.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted entity, or the first failure in the chain.</returns>
    public static Task<Result<TEntity>> SaveUpdateAsync<TEntity>(
        this IPersistentRepository<TEntity, Guid> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity<Guid>, ICreatable, IModifiable
        => Result<TEntity>.Ok(entity)
            .Bind(current => Result<TEntity>.Ok(current).MarkModified(nowUtc, actor))
            .ThenAsync((current, ct) => repository.UpdateAsync(current, ct), cancellationToken)
            .ThenAsync((current, ct) => unitOfWork.SaveChangesAsync(ct), cancellationToken);

    /// <summary>Applies the requested delete mode and commits it.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository to delete from.</param>
    /// <param name="unitOfWork">The unit of work that commits.</param>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="mode">Whether the row is soft or hard deleted.</param>
    /// <param name="nowUtc">The deletion instant.</param>
    /// <param name="actor">The actor performing the deletion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deleted entity, or the first failure in the chain.</returns>
    public static Task<Result<TEntity>> SaveDeleteAsync<TEntity>(
        this IPersistentRepository<TEntity, Guid> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        DeleteMode mode,
        DateTimeOffset nowUtc,
        string? actor,
        CancellationToken cancellationToken)
        where TEntity : class, IEntity<Guid>, ISoftDeletable
    {
        Result<TEntity> prepared = mode == DeleteMode.Hard
            ? Result<TEntity>.Ok(entity)
            : Result<TEntity>.Ok(entity).MarkDeleted(nowUtc, actor);

        return prepared
            .ThenAsync(
                (current, ct) => mode == DeleteMode.Hard
                    ? repository.RemoveAsync(current, ct)
                    : repository.UpdateAsync(current, ct),
                cancellationToken)
            .ThenAsync((current, ct) => unitOfWork.SaveChangesAsync(ct), cancellationToken);
    }
}
