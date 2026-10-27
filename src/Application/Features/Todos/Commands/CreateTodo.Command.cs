using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

using Core.Todos;

namespace Application.Features.Todos.Commands;

/// <summary>Creates a todo in the pending state.</summary>
/// <param name="Title">The todo title.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Priority">The urgency level.</param>
/// <param name="DueAtUtc">The optional UTC due date.</param>
/// <param name="ListId">The optional list identifier.</param>
/// <param name="CategoryIds">The optional category identifiers.</param>
public sealed record CreateTodoCommand(
    string? Title,
    string? Description = null,
    TodoPriority Priority = TodoConstant.Defaults.Priority,
    DateTimeOffset? DueAtUtc = null,
    Guid? ListId = null,
    IReadOnlyList<Guid>? CategoryIds = null) : ICommand<TodoModel>;
