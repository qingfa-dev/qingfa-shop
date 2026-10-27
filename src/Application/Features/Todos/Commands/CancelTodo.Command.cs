using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Cancels a todo that has not completed.</summary>
/// <param name="Id">The todo identifier.</param>
public sealed record CancelTodoCommand(Guid Id) : ICommand<TodoModel>;
