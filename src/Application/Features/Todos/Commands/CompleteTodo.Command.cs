using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Completes an in-progress todo.</summary>
/// <param name="Id">The todo identifier.</param>
public sealed record CompleteTodoCommand(Guid Id) : ICommand<TodoModel>;
