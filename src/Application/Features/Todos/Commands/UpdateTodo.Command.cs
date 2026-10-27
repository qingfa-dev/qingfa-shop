using Application.Common.Mediators.Commands;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Features.Todos.Commands;

/// <summary>Updates the mutable fields of a todo.</summary>
/// <remarks>
/// A <c>null</c> field leaves the current value unchanged. Use the explicit clear
/// flags to remove a description, due date, or list reference.
/// </remarks>
/// <param name="Id">The todo identifier.</param>
/// <param name="Title">The optional new title.</param>
/// <param name="Description">The optional new description.</param>
/// <param name="ClearDescription">Whether to clear the description.</param>
/// <param name="Priority">The optional new priority.</param>
/// <param name="DueAtUtc">The optional new UTC due date.</param>
/// <param name="ClearDueAt">Whether to clear the due date.</param>
/// <param name="ListId">The optional new list identifier.</param>
/// <param name="ClearList">Whether to clear the list reference.</param>
public sealed record UpdateTodoCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    bool ClearDescription = false,
    TodoPriority? Priority = null,
    DateTimeOffset? DueAtUtc = null,
    bool ClearDueAt = false,
    Guid? ListId = null,
    bool ClearList = false) : ICommand<TodoModel>;
