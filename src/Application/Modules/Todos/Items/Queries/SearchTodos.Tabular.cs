using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;
using Application.Modules.Todos.Common;
using Application.Modules.Todos.Items.Models;

namespace Application.Modules.Todos.Items.Queries;

public static class SearchTodosTabular
{
    #region Request
    public sealed record Request(QueryParameters Parameters, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response(
        IReadOnlyList<TabularColumn> Columns,
        IReadOnlyList<TabularRow> Rows)
        : TabularResult(Columns, Rows);
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request) : IQuery<Response>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IProjectionRepository<TodoReadModel, Guid> repository,
        IQueryMapProvider mapProvider,
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
                .Bind(tenantId => QueryBuilder<TodoReadModel>.FromParameters(
                        mapProvider,
                        request.Parameters)
                    .ApplyTodoScope(tenantId, request.IncludeDeleted)
                    .BuildTabular(TodosOutcome.Failure.EntityQueryUnsupported))
                .BindAsync(
                    (parsed, ct) => repository.QueryTabularAsync(parsed.Source, ct),
                    cancellationToken)
                .MapAsync(result => new Response(result.Columns, result.Rows));
        }
    }
    #endregion
}
