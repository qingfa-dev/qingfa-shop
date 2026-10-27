using Application.Common.Contexts;
using Application.Common.Mediators.Queries;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Pagination;
using Application.Features.Todos.Mapping;

using Contract.Common;
using Contract.Todos.Models;

using Core.Todos;

namespace Application.Features.Todos.Queries;

/// <summary>Handles <see cref="SearchTodosQuery"/>.</summary>
/// <param name="repository">The todo repository.</param>
/// <param name="mapProvider">The query map provider.</param>
/// <param name="tenantContext">The ambient tenant.</param>
public sealed class SearchTodosHandler(
    IPersistentRepository<Todo, Guid> repository,
    IQueryMapProvider mapProvider,
    ITenantContext tenantContext)
    : IQueryHandler<SearchTodosQuery, PageModel<TodoSummaryModel>>
{
    /// <inheritdoc />
    public async Task<Result<PageModel<TodoSummaryModel>>> Handle(
        SearchTodosQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await tenantContext.RequireTenant()
            .Bind(tenantId => QueryParser.Parse<Todo>(query.Parameters, mapProvider)
                .Ensure(parsed => !parsed.IsTabular, TodosOutcome.Failure.TabularQueryUnsupported)
                .Map(parsed => TodoQueryScope.Apply(parsed, tenantId, query.IncludeDeleted)))
            .BindAsync((parsed, ct) => repository.QueryAsync(parsed, ct), cancellationToken)
            .MapAsync(page => page.ToPage(todo => todo.ToSummary()));
    }
}
