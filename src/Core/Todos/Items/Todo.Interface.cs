namespace Core.Todos.Items;

/// <summary>Read-only contract for the todo aggregate.</summary>
public interface ITodo
{
    /// <summary>Gets the identifier of the todo.</summary>
    Guid Id { get; }

    /// <summary>Gets the title of the todo.</summary>
    string Title { get; }

    /// <summary>Gets the description of the todo.</summary>
    string? Description { get; }

    /// <summary>Gets the lifecycle status of the todo.</summary>
    TodoStatus Status { get; }

    /// <summary>Gets the priority of the todo.</summary>
    TodoPriority Priority { get; }

    /// <summary>Gets the UTC due date of the todo.</summary>
    DateTimeOffset? DueAtUtc { get; }

    /// <summary>Gets the UTC date the todo was completed.</summary>
    DateTimeOffset? CompletedAtUtc { get; }

    /// <summary>Gets the identifier of the list the todo belongs to.</summary>
    Guid? ListId { get; }

    /// <summary>Gets the identifiers of the categories the todo references.</summary>
    ICollection<Guid> CategoryIds { get; }

    /// <summary>Gets a value indicating whether the todo is completed.</summary>
    bool IsCompleted { get; }

    /// <summary>Gets the UTC date the todo was created.</summary>
    DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets the actor who created the todo.</summary>
    string? CreatedBy { get; }

    /// <summary>Gets the UTC date the todo was last modified.</summary>
    DateTimeOffset? ModifiedAtUtc { get; }

    /// <summary>Gets the actor who last modified the todo.</summary>
    string? ModifiedBy { get; }

    /// <summary>Gets a value indicating whether the todo is deleted.</summary>
    bool IsDeleted { get; }

    /// <summary>Gets the UTC date the todo was deleted.</summary>
    DateTimeOffset? DeletedAtUtc { get; }

    /// <summary>Gets the actor who deleted the todo.</summary>
    string? DeletedBy { get; }

    /// <summary>Gets the tenant the todo belongs to.</summary>
    Guid TenantId { get; }
}