namespace Application.BuildingBlocks.Querying;

/// <summary>Validation limits applied when a query is parsed or accepted.</summary>
/// <remarks>
/// A caller changing these is hardening (or loosening) the grammar — it has nothing
/// to do with how a query is executed.
/// </remarks>
public sealed record QueryParseLimits
{
    /// <summary>Default limits derived from <see cref="QueryConstant.Limits"/>; override individual properties to harden or loosen the grammar.</summary>
    public static QueryParseLimits Default { get; } = new();

    // Page

    /// <summary>Page size used when the request does not specify one.</summary>
    public int DefaultPageSize { get; init; } = QueryConstant.Limits.Page.DefaultSize;

    /// <summary>Largest page size a request may ask for.</summary>
    public int MaxPageSize     { get; init; } = QueryConstant.Limits.Page.MaxSize;

    // Filter

    /// <summary>Longest filter text (in characters) a request may send.</summary>
    public int MaxFilterLength { get; init; } = QueryConstant.Limits.Filter.MaxLength;

    /// <summary>Deepest parenthesis nesting a filter expression may use.</summary>
    public int MaxFilterDepth  { get; init; } = QueryConstant.Limits.Filter.MaxDepth;

    /// <summary>Most individual conditions a filter expression may contain.</summary>
    public int MaxConditions   { get; init; } = QueryConstant.Limits.Filter.MaxConditions;

    // Sort / select / group / aggregate

    /// <summary>Most fields a request may order by.</summary>
    public int MaxSortFields   { get; init; } = QueryConstant.Limits.Order.MaxFields;

    /// <summary>Most columns a request may project in a select clause.</summary>
    public int MaxSelectFields { get; init; } = QueryConstant.Limits.Select.MaxColumns;

    /// <summary>Most keys a request may group by.</summary>
    public int MaxGroupFields  { get; init; } = QueryConstant.Limits.Group.MaxKeys;

    /// <summary>Most aggregate expressions a request may ask for.</summary>
    public int MaxAggregates   { get; init; } = QueryConstant.Limits.Aggregate.MaxCount;

    // Search

    /// <summary>Longest search text (in characters) a request may send.</summary>
    public int MaxSearchLength   { get; init; } = QueryConstant.Limits.Search.MaxLength;

    /// <summary>Most individual terms a search text may contain.</summary>
    public int MaxSearchTerms    { get; init; } = QueryConstant.Limits.Search.MaxTerms;

    /// <summary>Most fields a search may target.</summary>
    public int MaxSearchFields   { get; init; } = QueryConstant.Limits.Search.MaxFields;
}
