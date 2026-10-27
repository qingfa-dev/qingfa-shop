using Core.Common.Domain.Entities;

namespace Application.Common.Persistence.Repositories;

/// <summary>Marker root that anchors every repository capability.</summary>
public interface IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
}

