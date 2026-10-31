using Application.BuildingBlocks.UseCases.Mediation.Queries;
using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Pagination;
using Application.Modules.Todos.Categories.Mappers;
using Application.Modules.Todos.Categories.Models;
using Application.Modules.Todos.Common;

using Core.Modules.Todos.Categories;

namespace Application.Modules.Todos.Categories.Queries;

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
        IMappableRepository<TodoCategory, Guid> repository,
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
                .Bind(tenantId => QueryBuilder<TodoCategory>.FromParameters(
                        mapProvider,
                        request.Parameters)
                    .ApplyTodoScope(tenantId, request.IncludeDeleted)
                    .BuildEntity(TodosOutcome.Failure.TabularQueryUnsupported))
                .BindAsync(
                    (parsed, ct) => repository.MapAsync(
                        parsed,
                        category => category.ToModel<Response>(),
                        ct),
                    cancellationToken);
        }
    }
    #endregion
}
