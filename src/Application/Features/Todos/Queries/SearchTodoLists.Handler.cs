using Application.Common.Contexts;
using Application.Common.Mediators.Queries;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Features.Todos.Mapping;

using Contract.Common;
using Contract.Todos.Models;

using Core.Todos.Lists;

namespace Application.Features.Todos.Queries;

/// <summary>Handles <see cref="SearchTodoListsQuery"/>.</summary>
/// <param name="repository">The list repository.</param>
/// <param name="mapProvider">The query map provider.</param>
/// <param name="tenantContext">The ambient tenant.</param>
public sealed class SearchTodoListsHandler(
    IPersistentRepository<TodoList, Guid> repository,
    IQueryMapProvider mapProvider,
    ITenantContext tenantContext)
    : IQueryHandler<SearchTodoListsQuery, PageModel<TodoListModel>>
{
    /// <inheritdoc />
    public async Task<Result<PageModel<TodoListModel>>> Handle(
        SearchTodoListsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await tenantContext.RequireTenant()
            .Bind(tenantId => QueryParser.Parse<TodoList>(query.Parameters, mapProvider)
                .Ensure(parsed => !parsed.IsTabular, TodosOutcome.Failure.TabularQueryUnsupported)
                .Map(parsed => TodoQueryScope.Apply(parsed, tenantId, query.IncludeDeleted)))
            .BindAsync((parsed, ct) => repository.QueryAsync(parsed, ct), cancellationToken)
            .MapAsync(page => page.ToPage(list => list.ToModel()));
    }
}
