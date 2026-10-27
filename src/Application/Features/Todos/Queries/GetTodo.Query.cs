using Application.Common.Mediators.Queries;

using Contract.Todos.Models;

namespace Application.Features.Todos.Queries;

/// <summary>Reads one todo within the ambient tenant.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="IncludeDeleted">Whether a soft-deleted todo may be returned.</param>
public sealed record GetTodoQuery(Guid Id, bool IncludeDeleted = false) : IQuery<TodoModel>;
