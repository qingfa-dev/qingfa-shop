using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Moves a pending todo into progress.</summary>
/// <param name="Id">The todo identifier.</param>
public sealed record StartTodoCommand(Guid Id) : ICommand<TodoModel>;
