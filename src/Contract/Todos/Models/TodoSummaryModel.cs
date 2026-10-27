using Core.Todos;

namespace Contract.Todos.Models;

/// <summary>Compact read model for todo list rows.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="Title">The todo title.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="Priority">The urgency level.</param>
/// <param name="DueAtUtc">The optional UTC due date.</param>
/// <param name="ListId">The optional list identifier.</param>
/// <param name="CategoryIds">The referenced category identifiers.</param>
/// <param name="IsCompleted">Whether the todo is completed.</param>
/// <param name="CreatedAtUtc">The UTC creation date.</param>
public sealed record TodoSummaryModel(
    Guid Id,
    string Title,
    TodoStatus Status,
    TodoPriority Priority,
    DateTimeOffset? DueAtUtc,
    Guid? ListId,
    IReadOnlyList<Guid> CategoryIds,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc);
