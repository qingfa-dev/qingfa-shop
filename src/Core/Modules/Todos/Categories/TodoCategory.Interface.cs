using Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Modifiable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;
using Core.BuildingBlocks.Domain.Aggregates;
using Core.BuildingBlocks.Domain.Events;

namespace Core.Modules.Todos.Categories;

/// <summary>Contract for the todo category aggregate.</summary>
public interface ITodoCategory : IAggregateRoot<IDomainEvent, Guid>, ICreatable, IModifiable, ISoftDeletable, ITenantable<Guid>
{
    /// <summary>Gets or sets the name of the category.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets the description of the category.</summary>
    string? Description { get; set; }
}