using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;

namespace Application.Features.Todos.Commands;

/// <summary>Deletes a todo list and clears the list reference on every todo.</summary>
/// <param name="Id">The list identifier.</param>
/// <param name="Mode">Whether to mark the row deleted or remove it permanently.</param>
public sealed record DeleteTodoListCommand(Guid Id, DeleteMode Mode = DeleteMode.Soft) : ICommand;
