using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.Modules.Todos.Items.Mappings;
using Application.Modules.Todos.Items.Models;

namespace Application.Modules.Todos.Items.Queries;

public static class GetTodo
{
    #region Request
    public sealed record Request(Guid Id, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response : TodoResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<Response>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoReadModel, Guid> repository,
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
                .MapAsync(todo => todo.ToModel<Response>());
        }
    }
    #endregion
}
