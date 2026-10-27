using Application.Common.Persistence.Repositories;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>Stages removals for the next IUnitOfWork.SaveChangesAsync (cruD).</summary>
public partial class EfCoreRepository<TEntity, TKey> : IDeletableRepository<TEntity, TKey>
{
    /// <inheritdoc />
    public Task<Result> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntity(entity);
        if (guard.IsFailure)
            return Task.FromResult(guard);

        try
        {
            // Cleanup: detach any stale tracked copy so the caller's instance becomes the single tracked entity.
            DetachTrackedCopy(entity);
            _context.Set<TEntity>().Remove(entity);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            // Translate: stage error to typed persistence outcome.
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Load: EF Core's change tracker requires the entity to be loaded before it can stage
    /// a deletion. FindAsync retrieves the entity (checking the identity cache first),
    /// so Remove can mark it for deletion in the next SaveChangesAsync call.
    /// </remarks>
    public async Task<Result> RemoveAsync(TKey id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Set<TEntity>().FindAsync(
                [id], cancellationToken: cancellationToken);

            if (entity is null)
                return Result.Fail(RepositoryOutcome.Failure.NotFoundFor<TEntity>(id!));

            _context.Set<TEntity>().Remove(entity);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            // Translate: stage error to typed persistence outcome.
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public Task<Result> RemoveRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntityCollection(entities);
        if (guard.IsFailure)
            return Task.FromResult(guard);

        try
        {
            _context.Set<TEntity>().RemoveRange(entities);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            // Translate: stage error to typed persistence outcome.
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }
}
