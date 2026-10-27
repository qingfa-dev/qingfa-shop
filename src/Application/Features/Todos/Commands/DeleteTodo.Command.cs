using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;

namespace Application.Features.Todos.Commands;

/// <summary>Deletes a todo, either softly or permanently.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="Mode">Whether to mark the row deleted or remove it permanently.</param>
public sealed record DeleteTodoCommand(Guid Id, DeleteMode Mode = DeleteMode.Soft) : ICommand;
