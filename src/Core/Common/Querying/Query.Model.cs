using Core.Common.Querying.Aggregation;
using Core.Common.Querying.Filtering;
using Core.Common.Querying.Grouping;
using Core.Common.Querying.Pagination;
using Core.Common.Querying.Projection;
using Core.Common.Querying.Sorting;

namespace Core.Common.Querying;

/// <summary>A complete, provider-neutral query.</summary>
public sealed record Query<T>(
    FilterNode? Filter,
    IReadOnlyList<SortField> OrderBy,
    PageRequest Page,
    string? Search,
    IReadOnlyList<SelectField>? Select,
    IReadOnlyList<GroupByField>? GroupBy,
    IReadOnlyList<AggregateSpec>? Aggregate)
{
    /// <summary>Gets a value indicating whether soft-deleted rows are included.</summary>
    public bool IncludeSoftDeleted { get; init; }

    /// <summary>Gets a value indicating whether the query returns tabular rows instead of entities.</summary>
    public bool IsTabular => Select is not null || GroupBy is not null || Aggregate is not null;
}