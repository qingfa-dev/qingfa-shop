using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Creates a todo list.</summary>
/// <param name="Name">The list name.</param>
/// <param name="Description">The optional description.</param>
public sealed record CreateTodoListCommand(string? Name, string? Description = null)
    : ICommand<TodoListModel>;
