using Application.Common.Mediators.Queries;
using Application.Common.Querying;

using Contract.Common;
using Contract.Todos.Models;

namespace Application.Features.Todos.Queries;

/// <summary>Searches todo lists with the provider-neutral query language.</summary>
/// <param name="Parameters">The filter, order, paging, and search parameters.</param>
/// <param name="IncludeDeleted">Whether soft-deleted lists may be returned.</param>
public sealed record SearchTodoListsQuery(QueryParameters Parameters, bool IncludeDeleted = false)
    : IQuery<PageModel<TodoListModel>>;
