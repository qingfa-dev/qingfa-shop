using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Domain.Aggregates;
using Core.Common.Domain.Events;

namespace Core.Todos.Items;

/// <summary>Contract for the todo aggregate.</summary>
public interface ITodoItem : IAggregateRoot<IDomainEvent, Guid>, ICreatable, IModifiable, ISoftDeletable, ITenantable<Guid>
{
    /// <summary>Gets or sets the title of the todo.</summary>
    string Title { get; set; }

    /// <summary>Gets or sets the description of the todo.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the lifecycle status of the todo.</summary>
    TodoStatus Status { get; set; }

    /// <summary>Gets or sets the priority of the todo.</summary>
    TodoPriority Priority { get; set; }

    /// <summary>Gets or sets the UTC due date of the todo.</summary>
    DateTimeOffset? DueAtUtc { get; set; }

    /// <summary>Gets or sets the UTC date the todo was completed.</summary>
    DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>Gets or sets the identifier of the list the todo belongs to.</summary>
    Guid? ListId { get; set; }

    /// <summary>Gets the identifiers of the categories the todo references.</summary>
    ICollection<Guid> CategoryIds { get; }

    /// <summary>Gets a value indicating whether the todo is completed.</summary>
    bool IsCompleted { get; }
}