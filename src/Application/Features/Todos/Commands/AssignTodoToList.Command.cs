using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Assigns a todo to a list.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="ListId">The list identifier.</param>
public sealed record AssignTodoToListCommand(Guid Id, Guid ListId) : ICommand<TodoModel>;
