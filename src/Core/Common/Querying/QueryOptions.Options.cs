namespace Core.Common.Querying;

/// <summary>Parser defaults and hard limits.</summary>
public sealed record QueryOptions
{
    public static QueryOptions Default { get; } = new();

    public int DefaultPageSize { get; init; } = QueryConstant.Limits.Page.DefaultSize;
    public int MaxPageSize { get; init; } = QueryConstant.Limits.Page.MaxSize;
    public int MaxFilterLength { get; init; } = QueryConstant.Limits.Filter.MaxLength;
    public int MaxFilterDepth { get; init; } = QueryConstant.Limits.Filter.MaxDepth;
    public int MaxConditions { get; init; } = QueryConstant.Limits.Filter.MaxConditions;
    public int MaxSortFields { get; init; } = QueryConstant.Limits.Order.MaxFields;
    public int MaxSelectFields { get; init; } = QueryConstant.Limits.Select.MaxColumns;
    public int MaxGroupFields { get; init; } = QueryConstant.Limits.Group.MaxKeys;
    public int MaxAggregates { get; init; } = QueryConstant.Limits.Aggregate.MaxCount;
    public int MaxSearchLength { get; init; } = QueryConstant.Limits.Search.MaxLength;
}