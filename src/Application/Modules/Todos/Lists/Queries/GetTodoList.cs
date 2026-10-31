using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.Modules.Todos.Lists.Models;
using Application.Modules.Todos.Lists.Mappings;

using Core.Modules.Todos.Lists;

namespace Application.Modules.Todos.Lists.Queries;

public static class GetTodoList
{
    #region Request
    public sealed record Request(Guid Id, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response : TodoListResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<Response>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoList, Guid> repository,
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
                .MapAsync(list => list.ToModel<Response>());
        }
    }
    #endregion
}
