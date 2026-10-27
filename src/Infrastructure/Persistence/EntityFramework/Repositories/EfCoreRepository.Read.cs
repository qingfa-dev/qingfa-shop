using Application.Common.Persistence.Repositories;

using Core.Common.Monad.Maybes;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.Repositories;

/// <summary>Reads a single entity by its key (cRud).</summary>
public partial class EfCoreRepository<TEntity, TKey> : IReadableRepository<TEntity, TKey>
{
    /// <inheritdoc />
    /// <remarks>
    /// Cache: FindAsync checks EF Core's identity cache first, returning the already-tracked
    /// entity without a database round-trip. This is useful when the entity was previously
    /// loaded in the same context scope.
    /// </remarks>
    public async Task<Result<TEntity>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Set<TEntity>().FindAsync(
                [id], cancellationToken: cancellationToken);

            if (entity is null)
                return Result<TEntity>.Fail(RepositoryOutcome.Failure.NotFoundFor<TEntity>(id!));

            return Result<TEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Read: AsNoTracking disables change tracking, avoiding the overhead of snapshot creation
    /// and state management for a read-only query that will not modify the entity.
    /// </remarks>
    public async Task<Result<Maybe<TEntity>>> FindByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Set<TEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken);

            Maybe<TEntity> maybe = entity is not null
                ? Maybe<TEntity>.From(entity)
                : Maybe<TEntity>.Empty();

            return Result<Maybe<TEntity>>.Success(maybe);
        }
        catch (Exception ex)
        {
            return Result<Maybe<TEntity>>.Fail(TranslateException(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        try
        {
            bool exists = await _context.Set<TEntity>()
                .AnyAsync(e => Equals(e.Id, id), cancellationToken);

            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(TranslateException(ex));
        }
    }
}
