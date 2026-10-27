using Application.Common.Persistence;
using Application.Common.Persistence.Repositories;

using Core.Common.Domain.Entities;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>Stages adds and updates for the next IUnitOfWork.SaveChangesAsync (CrUd).</summary>
public partial class EfCoreRepository<TEntity, TKey> : IWritableRepository<TEntity, TKey>
{
    /// <inheritdoc />
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
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
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
            return Result.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public Task<Result> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Result guard = RepositoryGuard.ValidateEntity(entity);
        if (guard.IsFailure)
            return Task.FromResult(guard);

        try
        {
            _context.Set<TEntity>().Update(entity);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }

    /// <inheritdoc />
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
            return Task.FromResult(Result.Fail(TranslateException(ex)));
        }
    }
}
