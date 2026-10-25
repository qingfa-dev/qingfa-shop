using Application.Common.Persistence.Repositories;

using Core.Common.Domain.Entities;
using Core.Common.Monad.Results;

namespace Application.Common.Persistence;

/// <summary>Stages adds and updates for the next <c>IUnitOfWork.SaveChangesAsync</c> (CrUd).</summary>
public interface IWritableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<Result> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<Result> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<Result> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}