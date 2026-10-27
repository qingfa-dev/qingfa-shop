using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

namespace Application.Features.Todos.Commands;

/// <summary>Restores a soft-deleted todo category.</summary>
/// <param name="Id">The category identifier.</param>
public sealed record RestoreTodoCategoryCommand(Guid Id) : ICommand<TodoCategoryModel>;
