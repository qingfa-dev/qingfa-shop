using Application.Common.Contexts;
using Application.Common.Mediators.Queries;
using Application.Common.Persistence.Repositories;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Features.Todos.Mapping;

using Contract.Common;
using Contract.Todos.Models;

using Core.Todos.Categories;

namespace Application.Features.Todos.Queries;

/// <summary>Handles <see cref="SearchTodoCategoriesQuery"/>.</summary>
/// <param name="repository">The category repository.</param>
/// <param name="mapProvider">The query map provider.</param>
/// <param name="tenantContext">The ambient tenant.</param>
public sealed class SearchTodoCategoriesHandler(
    IPersistentRepository<TodoCategory, Guid> repository,
    IQueryMapProvider mapProvider,
    ITenantContext tenantContext)
    : IQueryHandler<SearchTodoCategoriesQuery, PageModel<TodoCategoryModel>>
{
    /// <inheritdoc />
    public async Task<Result<PageModel<TodoCategoryModel>>> Handle(
        SearchTodoCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await tenantContext.RequireTenant()
            .Bind(tenantId => QueryParser.Parse<TodoCategory>(query.Parameters, mapProvider)
                .Ensure(parsed => !parsed.IsTabular, TodosOutcome.Failure.TabularQueryUnsupported)
                .Map(parsed => TodoQueryScope.Apply(parsed, tenantId, query.IncludeDeleted)))
            .BindAsync((parsed, ct) => repository.QueryAsync(parsed, ct), cancellationToken)
            .MapAsync(page => page.ToPage(category => category.ToModel()));
    }
}
