namespace Application.Common.Querying;

/// <summary>Validation limits applied when a query is parsed or accepted.</summary>
/// <remarks>
/// A caller changing these is hardening (or loosening) the grammar — it has nothing
/// to do with how a query is executed.
/// </remarks>
public sealed record QueryParseLimits
{
    public static QueryParseLimits Default { get; } = new();

    // Page
    public int DefaultPageSize { get; init; } = QueryConstant.Limits.Page.DefaultSize;
    public int MaxPageSize     { get; init; } = QueryConstant.Limits.Page.MaxSize;

    // Filter
    public int MaxFilterLength { get; init; } = QueryConstant.Limits.Filter.MaxLength;
    public int MaxFilterDepth  { get; init; } = QueryConstant.Limits.Filter.MaxDepth;
    public int MaxConditions   { get; init; } = QueryConstant.Limits.Filter.MaxConditions;

    // Sort / select / group / aggregate
    public int MaxSortFields   { get; init; } = QueryConstant.Limits.Order.MaxFields;
    public int MaxSelectFields { get; init; } = QueryConstant.Limits.Select.MaxColumns;
    public int MaxGroupFields  { get; init; } = QueryConstant.Limits.Group.MaxKeys;
    public int MaxAggregates   { get; init; } = QueryConstant.Limits.Aggregate.MaxCount;

    // Search
    public int MaxSearchLength   { get; init; } = QueryConstant.Limits.Search.MaxLength;
    public int MaxSearchTerms    { get; init; } = QueryConstant.Limits.Search.MaxTerms;
    public int MaxSearchFields   { get; init; } = QueryConstant.Limits.Search.MaxFields;
    public int MaxSearchLanguage { get; init; } = QueryConstant.Limits.Search.MaxLanguageLength;
}
