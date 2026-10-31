namespace Application.BuildingBlocks.Querying.Sorting;

/// <summary>One field in an ordering clause.</summary>
/// <param name="Field">The mapped field name to sort by (e.g., "CreatedAt", "Name").</param>
/// <param name="Direction">Sort direction: Ascending or Descending.</param>
/// <param name="Nulls">Where null values appear in the sorted result (None, HasValue, NotHasValue).</param>
public sealed record SortField(
    string Field,
    SortDirection Direction,
    SortNulls Nulls);