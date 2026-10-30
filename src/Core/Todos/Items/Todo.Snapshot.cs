namespace Core.Todos.Items;

/// <summary>Serializable snapshot shape for <see cref="Todo"/>.</summary>
public sealed record TodoSnapshot(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Description,
    TodoStatus Status,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? ListId,
    IReadOnlyList<Guid> CategoryIds,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy,
    long Version);