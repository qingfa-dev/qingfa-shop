using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Restores a soft-deleted todo.</summary>
/// <param name="Id">The todo identifier.</param>
public sealed record RestoreTodoCommand(Guid Id) : ICommand<TodoModel>;
