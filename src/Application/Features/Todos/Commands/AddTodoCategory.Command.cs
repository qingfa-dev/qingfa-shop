using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Adds a category reference to a todo.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="CategoryId">The category identifier.</param>
public sealed record AddTodoCategoryCommand(Guid Id, Guid CategoryId) : ICommand<TodoModel>;
