using Core.Common.Concerns.Foundation.Identifiable;

namespace Core.Common.Domain.Entities;

public interface IEntity<out TId> : IIdentifiable<TId>
{

}