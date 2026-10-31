using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Projection;
using Application.Modules.Todos.Common;

using Core.Modules.Todos.Categories;

namespace Application.Modules.Todos.Categories.Queries;

public static class SearchTodoCategoriesTabular
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
        IProjectionRepository<TodoCategory, Guid> repository,
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
                .Bind(tenantId => QueryBuilder<TodoCategory>.FromParameters(
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
