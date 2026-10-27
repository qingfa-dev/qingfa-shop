using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Reopens a finished todo and clears its completion date.</summary>
/// <param name="Id">The todo identifier.</param>
public sealed record ReopenTodoCommand(Guid Id) : ICommand<TodoModel>;
