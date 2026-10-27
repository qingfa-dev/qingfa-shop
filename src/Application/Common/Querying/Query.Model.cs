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
    public FilterNode? Filter { get; init; }
    public SearchRequest? Search { get; init; }
    public IReadOnlyList<SortField> OrderBy { get; init; } = Array.Empty<SortField>();

    /// <summary>Null for queries that are not paged (single, count, exists, stream).</summary>
    public PageRequest? Page { get; init; }

    // ---- Projection ----------------------------------------------------
    public IReadOnlyList<SelectField>? Select { get; init; }
    public IReadOnlyList<GroupByField>? GroupBy { get; init; }
    public IReadOnlyList<AggregateSpec>? Aggregate { get; init; }

    // ---- Cross-cutting -------------------------------------------------
    public QueryExecutionOptions Options { get; init; } = QueryExecutionOptions.Default;
    public QueryParseLimits Limits { get; init; } = QueryParseLimits.Default;

    // ---- Dispatch ------------------------------------------------------
    public QueryShape Shape =>
        Aggregate is not null && GroupBy is null ? QueryShape.ScalarAggregate
        : Select is not null || GroupBy is not null || Aggregate is not null ? QueryShape.Tabular
        : QueryShape.Entity;

    public bool IsTabular => Shape != QueryShape.Entity;
}

public enum QueryShape
{
    Entity,
    Tabular,
    ScalarAggregate,
}