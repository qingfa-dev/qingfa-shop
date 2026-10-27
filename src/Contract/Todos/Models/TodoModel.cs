using Core.Todos.Items;

namespace Contract.Todos.Models;

/// <summary>Read model for a single todo, including lifecycle and audit state.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="TenantId">The tenant that owns the todo.</param>
/// <param name="Title">The todo title.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="Priority">The urgency level.</param>
/// <param name="DueAtUtc">The optional UTC due date.</param>
/// <param name="CompletedAtUtc">The optional UTC completion date.</param>
/// <param name="ListId">The optional list identifier.</param>
/// <param name="CategoryIds">The referenced category identifiers.</param>
/// <param name="IsCompleted">Whether the todo is completed.</param>
/// <param name="CreatedAtUtc">The UTC creation date.</param>
/// <param name="CreatedBy">The optional creator.</param>
/// <param name="ModifiedAtUtc">The optional UTC last-modification date.</param>
/// <param name="ModifiedBy">The optional last modifier.</param>
/// <param name="IsDeleted">Whether the todo is soft-deleted.</param>
/// <param name="DeletedAtUtc">The optional UTC deletion date.</param>
/// <param name="DeletedBy">The optional deleter.</param>
public sealed record TodoModel(
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
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedBy,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletedBy);
