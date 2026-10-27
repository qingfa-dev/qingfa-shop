using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Restores a soft-deleted todo list.</summary>
/// <param name="Id">The list identifier.</param>
public sealed record RestoreTodoListCommand(Guid Id) : ICommand<TodoListModel>;
