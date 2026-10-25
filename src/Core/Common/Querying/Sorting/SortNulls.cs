namespace Core.Common.Querying.Sorting;

/// <summary>Optional null-state ordering requested by <c>field?</c> or <c>field!</c>.</summary>
public enum SortNulls
{
    None,
    HasValue,
    NotHasValue,
}