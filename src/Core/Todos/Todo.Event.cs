using Core.Common.Domain.Events;

namespace Core.Todos;

/// <summary>Raised when a todo is created.</summary>
public sealed record TodoCreated : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="title">The title of the todo.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoCreated(Guid todoId, Guid tenantId, string title, IMetadata? metadata = null)
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
public sealed record TodoStarted : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoStarted(Guid todoId, Guid tenantId, IMetadata? metadata = null)
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
public sealed record TodoCompleted : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="completedAtUtc">The UTC date the todo was completed.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoCompleted(Guid todoId, Guid tenantId, DateTimeOffset completedAtUtc, IMetadata? metadata = null)
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
public sealed record TodoCancelled : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoCancelled(Guid todoId, Guid tenantId, IMetadata? metadata = null)
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
public sealed record TodoReopened : DomainEvent
{
    /// <summary>Initializes the event.</summary>
    /// <param name="todoId">The identifier of the todo.</param>
    /// <param name="tenantId">The tenant the todo belongs to.</param>
    /// <param name="metadata">The optional metadata to merge with this event.</param>
    public TodoReopened(Guid todoId, Guid tenantId, IMetadata? metadata = null)
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