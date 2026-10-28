using Application.Common.Mediators.Queries;
using Application.Common.Persistence.Repositories;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos.Lists;

namespace Application.Features.Todos.Queries;

/// <summary>Handles <see cref="GetTodoListQuery"/>.</summary>
/// <param name="repository">The list repository.</param>
/// <param name="tenantContext">The ambient tenant.</param>
public sealed class GetTodoListHandler(
    IPersistentRepository<TodoList, Guid> repository,
    ITenantContext tenantContext)
    : IQueryHandler<GetTodoListQuery, TodoListModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoListModel>> Handle(
        GetTodoListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await tenantContext.RequireTenant()
            .BindAsync(
                (tenantId, ct) => repository.FindOwnedAsync(
                    query.Id,
                    tenantId,
                    ct,
                    query.IncludeDeleted),
                cancellationToken)
            .MapAsync(list => list.ToModel());
    }
}
