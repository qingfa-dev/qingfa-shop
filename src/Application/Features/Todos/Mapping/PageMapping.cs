using Application.Common.Querying.Pagination;

using Contract.Common;

namespace Application.Features.Todos.Mapping;

/// <summary>Hand-written mapping from a repository page to a contract page.</summary>
public static class PageMapping
{
    /// <summary>Maps one entity page to a model page.</summary>
    /// <typeparam name="TEntity">The entity type in the repository page.</typeparam>
    /// <typeparam name="TModel">The model type in the returned page.</typeparam>
    /// <param name="page">The repository page.</param>
    /// <param name="mapper">The per-item mapper.</param>
    /// <returns>The mapped page.</returns>
    public static PageModel<TModel> ToPage<TEntity, TModel>(
        this QueryPage<TEntity> page,
        Func<TEntity, TModel> mapper)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(mapper);

        return new PageModel<TModel>(
            [.. page.Items.Select(mapper)],
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}
