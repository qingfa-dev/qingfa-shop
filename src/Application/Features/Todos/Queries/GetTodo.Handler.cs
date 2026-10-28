using Application.Common.Mediators.Queries;
using Application.Common.Persistence.Repositories;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos.Items;

namespace Application.Features.Todos.Queries;

/// <summary>Handles <see cref="GetTodoQuery"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="tenantContext">The ambient tenant.</param>
public sealed class GetTodoHandler(
    IPersistentRepository<Todo, Guid> repository,
    ITenantContext tenantContext)
    : IQueryHandler<GetTodoQuery, TodoModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoModel>> Handle(
        GetTodoQuery query,
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
            .MapAsync(todo => todo.ToModel());
    }
}
