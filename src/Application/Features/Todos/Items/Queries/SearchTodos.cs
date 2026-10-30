using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;

namespace Application.Features.Todos.Items.Queries;

public static class SearchTodos
{
    #region Request
    public sealed record Request(QueryParameters Parameters, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response : TodoSummaryResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request)
        : IQuery<QueryPage<Response>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IMappableRepository<TodoReadModel, Guid> repository,
        IQueryMapProvider mapProvider,
        ITenantContext tenantContext)
        : IQueryHandler<UseCase, QueryPage<Response>>
    {
        public async Task<Result<QueryPage<Response>>> Handle(
            UseCase query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(query.Request);

            Request request = query.Request;

            return await tenantContext.RequireTenant()
                .Bind(tenantId => QueryBuilder<TodoReadModel>.FromParameters(
                        mapProvider,
                        request.Parameters)
                    .ApplyTodoScope(tenantId, request.IncludeDeleted)
                    .BuildEntity(TodosOutcome.Failure.TabularQueryUnsupported))
                .BindAsync(
                    (parsed, ct) => repository.MapAsync(
                        parsed,
                        todo => todo.ToSummary<Response>(),
                        ct),
                    cancellationToken);
        }
    }
    #endregion
}
