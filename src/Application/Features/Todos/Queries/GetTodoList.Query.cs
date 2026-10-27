using Application.Common.Mediators.Queries;

using Contract.Todos.Models;

namespace Application.Features.Todos.Queries;

/// <summary>Reads one todo list within the ambient tenant.</summary>
/// <param name="Id">The list identifier.</param>
/// <param name="IncludeDeleted">Whether a soft-deleted list may be returned.</param>
public sealed record GetTodoListQuery(Guid Id, bool IncludeDeleted = false)
    : IQuery<TodoListModel>;
