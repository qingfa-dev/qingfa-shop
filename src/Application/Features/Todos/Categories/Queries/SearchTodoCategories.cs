using Application.Common.Mediators.Queries;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Categories.Mappers;
using Application.Features.Todos.Categories.Models;
using Application.Features.Todos.Common;
using Application.Features.Todos.Mapping;

using Core.Todos.Categories;

namespace Application.Features.Todos.Categories.Queries;

public static class SearchTodoCategories
{
    #region Request
    public sealed record Request(QueryParameters Parameters, bool IncludeDeleted = false);
    #endregion

    #region Response
    public sealed record Response : TodoCategoryResponseModel;
    #endregion

    #region UseCase
    public sealed record UseCase(Request Request)
        : IQuery<QueryPage<Response>>;
    #endregion

    #region Handler
    public sealed class UseCaseHandler(
        IPersistentRepository<TodoCategory, Guid> repository,
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
                .Bind(tenantId => QueryParser.Parse<TodoCategory>(
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
                    [.. page.Items.Select(category => category.ToModel<Response>())],
                    page.Page,
                    page.PageSize,
                    page.TotalCount));
        }
    }
    #endregion
}
