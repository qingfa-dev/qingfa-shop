using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Updates the mutable fields of a todo list.</summary>
/// <remarks>A <c>null</c> field leaves the current value unchanged.</remarks>
/// <param name="Id">The list identifier.</param>
/// <param name="Name">The optional new name.</param>
/// <param name="Description">The optional new description.</param>
/// <param name="ClearDescription">Whether to clear the description.</param>
public sealed record UpdateTodoListCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    bool ClearDescription = false) : ICommand<TodoListModel>;
