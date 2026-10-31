using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Projection;
using Application.BuildingBlocks.Querying.Sorting;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Turns a neutral query into something the provider can execute.</summary>
/// <remarks>
/// The contract lives beside the adapter that implements it, because the shape of
/// a translated query is the provider's own: a queryable, a projection, a row
/// set. A caller depends on this interface rather than on one implementation, so
/// a different adapter can be substituted without the caller naming it.
/// <para>
/// Every member reports a failure as a value. A translation that cannot be built
/// is a neutral error, never a thrown exception.
/// </para>
/// </remarks>
/// <typeparam name="T">The entity the query addresses.</typeparam>
public interface IQueryTranslator<T> where T : class
{
    /// <summary>Applies a filter tree to a source query.</summary>
    /// <param name="source">The query to filter.</param>
    /// <param name="filter">The filter tree, or null for no filter.</param>
    /// <param name="map">The field map the filter was built against.</param>
    /// <returns>The filtered query, or a failure describing why it cannot be built.</returns>
    Result<IQueryable<T>> ApplyFilter(
        IQueryable<T> source,
        FilterNode? filter,
        QueryMap<T> map);

    /// <summary>Applies a free-text search across every searchable field.</summary>
    /// <param name="source">The query to search.</param>
    /// <param name="search">The search text, or null for no search.</param>
    /// <param name="map">The field map the search was built against.</param>
    /// <returns>The searched query, or a failure describing why it cannot be built.</returns>
    Result<IQueryable<T>> ApplySearch(IQueryable<T> source, string? search, QueryMap<T> map);

    /// <summary>Applies an ordering to a source query.</summary>
    /// <param name="source">The query to order.</param>
    /// <param name="order">The sort fields.</param>
    /// <param name="map">The field map the sort was built against.</param>
    /// <returns>The ordered query, or a failure describing why it cannot be built.</returns>
    Result<IQueryable<T>> ApplySort(
        IQueryable<T> source,
        IReadOnlyList<SortField> order,
        QueryMap<T> map);

    /// <summary>Applies a page window to a source query.</summary>
    /// <param name="source">The query to page.</param>
    /// <param name="page">The page window.</param>
    /// <returns>The paged query.</returns>
    Result<IQueryable<T>> ApplyPaging(IQueryable<T> source, PageRequest page);

    /// <summary>Eager-loads the navigations a grouped or aggregated shape reads from.</summary>
    /// <param name="source">The query to load navigations for.</param>
    /// <param name="query">The query whose shape decides the navigations.</param>
    /// <param name="map">The field map the shape was built against.</param>
    /// <returns>The query with the navigations included.</returns>
    IQueryable<T> IncludeNavigations(
        IQueryable<T> source,
        Query<T> query,
        QueryMap<T> map);

    /// <summary>Builds a server-side row projection for a select-only query.</summary>
    /// <param name="source">The query to project.</param>
    /// <param name="select">The selected columns.</param>
    /// <param name="map">The field map the projection was built against.</param>
    /// <returns>The projection plan, or a failure describing why it cannot be built.</returns>
    Result<TabularPlan> BuildSelectPlan(
        IQueryable<T> source,
        IReadOnlyList<SelectField> select,
        QueryMap<T> map);

    /// <summary>Evaluates group and aggregate shapes over an already-filtered row set.</summary>
    /// <param name="rows">The materialized rows to evaluate.</param>
    /// <param name="query">The query whose shape decides the result.</param>
    /// <param name="map">The field map the shape was built against.</param>
    /// <returns>The tabular result, or a failure describing why it cannot be built.</returns>
    Result<TabularResult> BuildTabularClientSide(
        IReadOnlyList<T> rows,
        Query<T> query,
        QueryMap<T> map);
}