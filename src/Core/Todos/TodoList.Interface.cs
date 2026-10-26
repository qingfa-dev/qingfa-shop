using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

namespace Core.Todos;

/// <summary>Contract for the todo list aggregate.</summary>
public interface ITodoList : IAggregateRoot<IDomainEvent, Guid>, ICreatable, IModifiable, ISoftDeletable, ITenantable<Guid>
{
    /// <summary>Gets or sets the name of the list.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets the description of the list.</summary>
    string? Description { get; set; }
}