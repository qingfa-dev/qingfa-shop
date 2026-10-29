using Application.Common.Mediators.Queries;
using Application.Features.Todos.Categories.Mappers;
using Application.Features.Todos.Categories.Models;

using Core.Todos.Categories;

namespace Application.Features.Todos.Categories.Queries;

public static class GetTodoCategory
{
    #region Request
    public sealed record Request(Guid Id, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response : TodoCategoryResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<Response>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoCategory, Guid> repository,
        ITenantContext tenantContext)
        : IQueryHandler<UseCase, Response>
    {
        public async Task<Result<Response>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            Request request = query.Request;

            return await tenantContext.RequireTenant()
                .BindAsync(
                    (tenantId, ct) => repository.FindOwnedAsync(
                        request.Id,
                        tenantId,
                        ct,
                        request.IncludeDeleted),
                    cancellationToken)
                .MapAsync(category => category.ToModel<Response>());
        }
    }
    #endregion
}
