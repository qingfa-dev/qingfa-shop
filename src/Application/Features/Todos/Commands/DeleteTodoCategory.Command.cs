using Application.Common.Mediators.Commands;
using Application.Common.Persistence.Repositories;

namespace Application.Features.Todos.Commands;

/// <summary>Deletes a todo category and removes its reference from every todo.</summary>
/// <param name="Id">The category identifier.</param>
/// <param name="Mode">Whether to mark the row deleted or remove it permanently.</param>
public sealed record DeleteTodoCategoryCommand(
    Guid Id,
    DeleteMode Mode = DeleteMode.Soft) : ICommand;
