namespace Core.Common.Querying.Sorting;

/// <summary>One field in an ordering clause.</summary>
public sealed record SortField(
    string Field,
    SortDirection Direction,
    SortNulls Nulls);