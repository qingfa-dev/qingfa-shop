using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Pagination;
using Application.Common.Querying.Projection;
using Application.Common.Querying.Searching;
using Application.Common.Querying.Sorting;

namespace Application.Common.Querying;

/// <summary>A complete, provider-neutral query.</summary>
public sealed record Query<T>
{
    // ---- Intent --------------------------------------------------------

    /// <summary>Optional filter tree (conditions and groups) that restricts which rows match the query.</summary>
    public FilterNode? Filter { get; init; }

    /// <summary>Optional full-text or basic search request applied to string fields.</summary>
    public SearchRequest? Search { get; init; }

    /// <summary>Ordered list of sort fields; first element is the primary sort, subsequent fields break ties.</summary>
    public IReadOnlyList<SortField> OrderBy { get; init; } = Array.Empty<SortField>();

    /// <summary>Null for queries that are not paged (single, count, exists, stream).</summary>
    public PageRequest? Page { get; init; }

    // ---- Projection ----------------------------------------------------

    /// <summary>When present, only the listed fields are projected instead of returning full entities.</summary>
    public IReadOnlyList<SelectField>? Select { get; init; }

    /// <summary>When present, results are grouped by the listed fields before projection or aggregation.</summary>
    public IReadOnlyList<GroupByField>? GroupBy { get; init; }

    /// <summary>When present, aggregate functions (Count, Sum, etc.) are computed over the result set.</summary>
    public IReadOnlyList<AggregateSpec>? Aggregate { get; init; }

    // ---- Cross-cutting -------------------------------------------------

    /// <summary>Runtime execution hints such as distinct, split query, max rows, and command timeout.</summary>
    public QueryExecutionOptions Options { get; init; } = QueryExecutionOptions.Default;

    /// <summary>Parse-time limits that guard against overly complex query strings (depth, conditions, etc.).</summary>
    public QueryParseLimits Limits { get; init; } = QueryParseLimits.Default;

    // ---- Dispatch ------------------------------------------------------

    /// <summary>
    /// Derives the query shape from the projection fields to route to the correct execution path.
    /// Aggregate without GroupBy produces a single scalar; Select, GroupBy, or Aggregate with GroupBy produce tabular results;
    /// otherwise the query returns full entities.
    /// </summary>
    public QueryShape Shape =>
        Aggregate is not null && GroupBy is null ? QueryShape.ScalarAggregate
        : Select is not null || GroupBy is not null || Aggregate is not null ? QueryShape.Tabular
        : QueryShape.Entity;

    /// <summary>True when the query shape is Tabular or ScalarAggregate (not Entity).</summary>
    public bool IsTabular => Shape != QueryShape.Entity;
}

/// <summary>
/// Determines which execution path a query takes in the repository.
/// Entity returns full domain objects, Tabular returns projected rows, ScalarAggregate returns a single numeric value.
/// </summary>
public enum QueryShape
{
    /// <summary>Return full entity objects; no projection, grouping, or aggregation is applied.</summary>
    Entity,

    /// <summary>Return projected rows (select fields, group-by keys, or grouped aggregates) as tabular data.</summary>
    Tabular,

    /// <summary>Return a single scalar value from an aggregate function without a group-by clause.</summary>
    ScalarAggregate,
}