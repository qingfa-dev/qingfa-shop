using Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Modifiable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.BuildingBlocks.Domain.Aggregates;
using Core.BuildingBlocks.Domain.Events;

namespace Core.Modules.Todos.Lists;

/// <summary>Contract for the todo list aggregate.</summary>
public interface ITodoList : IAggregateRoot<IDomainEvent, Guid>, ICreatable, IModifiable, ISoftDeletable, ITenantable<Guid>
{
    /// <summary>Gets or sets the name of the list.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets the description of the list.</summary>
    string? Description { get; set; }
}