using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Common;
using Application.Features.Todos.Lists.Models;
using Application.Features.Todos.Mapping;

using Core.Todos.Lists;

namespace Application.Features.Todos.Lists.Queries;

public static class SearchTodoLists
{
    #region Request
    public sealed record Request(QueryParameters Parameters, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response : TodoListResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request)
        : IQuery<QueryPage<Response>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoList, Guid> repository,
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
                .Bind(tenantId => QueryParser.Parse<TodoList>(
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
                    [.. page.Items.Select(list => list.ToModel<Response>())],
                    page.Page,
                    page.PageSize,
                    page.TotalCount));
        }
    }
    #endregion
}
