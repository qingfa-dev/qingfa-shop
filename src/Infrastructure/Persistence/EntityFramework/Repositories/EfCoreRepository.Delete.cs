using Application.Common.Persistence;
using Application.Common.Persistence.Repositories;

using Core.Common.Domain.Entities;

using Microsoft.EntityFrameworkCore;

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
            _context.Set<TEntity>().Remove(entity);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }

    /// <inheritdoc />
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
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }
}
