using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Removes a todo from its list.</summary>
/// <param name="Id">The todo identifier.</param>
public sealed record ClearTodoListCommand(Guid Id) : ICommand<TodoModel>;
