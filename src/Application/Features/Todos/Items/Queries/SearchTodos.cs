using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Common;
using Application.Features.Todos.Items.Mappings;
using Application.Features.Todos.Items.Models;

using Core.Todos.Items;

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
        IPersistentRepository<Todo, Guid> repository,
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
                .Bind(tenantId => QueryParser.Parse<Todo>(
                        request.Parameters,
                        mapProvider)
                    .Ensure(
                        parsed => !parsed.IsTabular,
                        TodosOutcome.Failure.TabularQueryUnsupported)
                    .Map(parsed => TodoQueryScope.Apply(
                        parsed,
                        tenantId,
                        request.IncludeDeleted)))
                .BindAsync(
                    (parsed, ct) => repository.QueryAsync(parsed, ct),
                    cancellationToken)
                .MapAsync(page => new QueryPage<Response>(
                    [.. page.Items.Select(todo => todo.ToSummary<Response>())],
                    page.Page,
                    page.PageSize,
                    page.TotalCount));
        }
    }
    #endregion
}
