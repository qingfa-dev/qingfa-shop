using Core.BuildingBlocks.Modeling.Concerns.Foundation.Identifiable;

namespace Core.BuildingBlocks.Domain.Entities;

public interface IEntity<out TId> : IIdentifiable<TId>
{

}