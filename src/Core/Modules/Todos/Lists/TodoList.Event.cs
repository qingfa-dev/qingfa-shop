using Core.BuildingBlocks.Domain.Events;

namespace Core.Modules.Todos.Lists;

/// <summary>Raised when a todo list is created.</summary>
public sealed record TodoListCreated : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="listId">The identifier of the list.</param>
    /// <param name="tenantId">The tenant the list belongs to.</param>
    /// <param name="name">The name of the list.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoListCreated(Guid listId, Guid tenantId, string name, DefaultMetadata? metadata = null)
        : base(metadata: metadata)
    {
        ListId = listId;
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>Gets the identifier of the list.</summary>
    public Guid ListId { get; }

    /// <summary>Gets the tenant the list belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the name of the list.</summary>
    public string Name { get; }
}

/// <summary>Raised when a todo list is renamed.</summary>
public sealed record TodoListRenamed : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="listId">The identifier of the list.</param>
    /// <param name="tenantId">The tenant the list belongs to.</param>
    /// <param name="name">The new name of the list.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoListRenamed(Guid listId, Guid tenantId, string name, DefaultMetadata? metadata = null)
        : base(metadata: metadata)
    {
        ListId = listId;
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>Gets the identifier of the list.</summary>
    public Guid ListId { get; }

    /// <summary>Gets the tenant the list belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the new name of the list.</summary>
    public string Name { get; }
}