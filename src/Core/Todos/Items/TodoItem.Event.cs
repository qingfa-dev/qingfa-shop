using Core.Common.Domain.Events;

namespace Core.Todos.Items;

/// <summary>Raised when a todo is created.</summary>
public sealed record TodoItemCreated : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="title">The title of the todo.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoItemCreated(Guid todoId, Guid tenantId, string title, IMetadata? metadata = null)
        : base(metadata: metadata)
    {
        TodoId = todoId;
        TenantId = tenantId;
        Title = title;
    }

    /// <summary>Gets the identifier of the todo.</summary>
    public Guid TodoId { get; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the title of the todo.</summary>
    public string Title { get; }
}

/// <summary>Raised when a todo moves into progress.</summary>
public sealed record TodoItemStarted : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoItemStarted(Guid todoId, Guid tenantId, IMetadata? metadata = null)
        : base(metadata: metadata)
    {
        TodoId = todoId;
        TenantId = tenantId;
    }

    /// <summary>Gets the identifier of the todo.</summary>
    public Guid TodoId { get; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    public Guid TenantId { get; }
}

/// <summary>Raised when a todo is completed.</summary>
public sealed record TodoItemCompleted : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="completedAtUtc">The UTC date the todo was completed.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoItemCompleted(Guid todoId, Guid tenantId, DateTimeOffset completedAtUtc, IMetadata? metadata = null)
        : base(metadata: metadata)
    {
        TodoId = todoId;
        TenantId = tenantId;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>Gets the identifier of the todo.</summary>
    public Guid TodoId { get; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the UTC date the todo was completed.</summary>
    public DateTimeOffset CompletedAtUtc { get; }
}

/// <summary>Raised when a todo is cancelled.</summary>
public sealed record TodoItemCancelled : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoItemCancelled(Guid todoId, Guid tenantId, IMetadata? metadata = null)
        : base(metadata: metadata)
    {
        TodoId = todoId;
        TenantId = tenantId;
    }

    /// <summary>Gets the identifier of the todo.</summary>
    public Guid TodoId { get; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    public Guid TenantId { get; }
}

/// <summary>Raised when a finished todo is reopened.</summary>
public sealed record TodoItemReopened : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoItemReopened(Guid todoId, Guid tenantId, IMetadata? metadata = null)
        : base(metadata: metadata)
    {
        TodoId = todoId;
        TenantId = tenantId;
    }

    /// <summary>Gets the identifier of the todo.</summary>
    public Guid TodoId { get; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    public Guid TenantId { get; }
}