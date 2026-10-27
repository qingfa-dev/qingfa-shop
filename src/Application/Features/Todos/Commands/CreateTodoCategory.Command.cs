using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Creates a todo category.</summary>
/// <param name="Name">The category name.</param>
/// <param name="Description">The optional description.</param>
public sealed record CreateTodoCategoryCommand(string? Name, string? Description = null)
    : ICommand<TodoCategoryModel>;
