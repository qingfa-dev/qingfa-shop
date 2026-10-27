using Application.Common.Persistence.Repositories;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>Stages adds and updates for the next IUnitOfWork.SaveChangesAsync (CrUd).</summary>
public partial class EfCoreRepository<TEntity, TKey> : IWritableRepository<TEntity, TKey>
{
    /// <inheritdoc />
    /// <remarks>
    /// Stage: entities are added to the change tracker but not persisted yet.
    /// The unit of work pattern defers persistence until SaveChangesAsync is called,
    /// allowing multiple operations to be committed atomically.
    /// </remarks>
    public async Task<Result> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntity(entity);
        if (guard.IsFailure)
            return guard;

        try
        {
            await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            // Translate: stage error to typed persistence outcome.
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stage: entities are added to the change tracker but not persisted yet.
    /// The unit of work pattern defers persistence until SaveChangesAsync is called.
    /// </remarks>
    public async Task<Result> AddRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntityCollection(entities);
        if (guard.IsFailure)
            return guard;

        try
        {
            _context.Set<TEntity>().AddRange(entities);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            // Translate: stage error to typed persistence outcome.
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cleanup: DetachTrackedCopy prevents EF Core from rejecting a second instance with the same key.
    /// Since reads use AsNoTracking, updates receive detached copies that must replace any
    /// stale tracked entity before EF Core can attach them.
    /// </remarks>
    public Task<Result> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntity(entity);
        if (guard.IsFailure)
            return Task.FromResult(guard);

        try
        {
            // Cleanup: detach any stale tracked copy so the caller's instance becomes the single tracked entity.
            DetachTrackedCopy(entity);
            _context.Set<TEntity>().Update(entity);
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
    /// Stage: entities are updated in the change tracker but not persisted yet.
    /// The unit of work pattern defers persistence until SaveChangesAsync is called.
    /// </remarks>
    public Task<Result> UpdateRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntityCollection(entities);
        if (guard.IsFailure)
            return Task.FromResult(guard);

        try
        {
            _context.Set<TEntity>().UpdateRange(entities);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            // Translate: stage error to typed persistence outcome.
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }
}
