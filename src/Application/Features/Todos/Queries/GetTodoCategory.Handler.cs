using Application.Common.Mediators.Queries;
using Application.Common.Persistence.Repositories;
using Application.Common.Security.Authentication.Contexts;
using Application.Features.Todos.Mapping;

using Contract.Todos.Models;

using Core.Todos.Categories;

namespace Application.Features.Todos.Queries;

/// <summary>Handles <see cref="GetTodoCategoryQuery"/>.</summary>
/// <param name="repository">The category repository.</param>
/// <param name="tenantContext">The ambient tenant.</param>
public sealed class GetTodoCategoryHandler(
    IPersistentRepository<TodoCategory, Guid> repository,
    ITenantContext tenantContext)
    : IQueryHandler<GetTodoCategoryQuery, TodoCategoryModel>
{
    /// <inheritdoc />
    public async Task<Result<TodoCategoryModel>> Handle(
        GetTodoCategoryQuery query,
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
            .MapAsync(category => category.ToModel());
    }
}
