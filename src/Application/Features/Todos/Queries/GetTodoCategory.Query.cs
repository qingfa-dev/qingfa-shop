using Application.Common.Mediators.Queries;

using Contract.Todos.Models;

namespace Application.Features.Todos.Queries;

/// <summary>Reads one todo category within the ambient tenant.</summary>
/// <param name="Id">The category identifier.</param>
/// <param name="IncludeDeleted">Whether a soft-deleted category may be returned.</param>
public sealed record GetTodoCategoryQuery(Guid Id, bool IncludeDeleted = false)
    : IQuery<TodoCategoryModel>;
