using Core.BuildingBlocks.Domain.Entities;

namespace Application.BuildingBlocks.Persistence.Repositories;

/// <summary>Stages removals for the next <c>IUnitOfWork.SaveChangesAsync</c> (cruD).</summary>
public interface IDeletableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<Result> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<Result> RemoveAsync(TKey id, CancellationToken cancellationToken = default);
    Task<Result> RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}