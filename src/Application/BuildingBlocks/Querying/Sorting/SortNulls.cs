namespace Application.BuildingBlocks.Querying.Sorting;

/// <summary>Optional null-state ordering requested by <c>field?</c> or <c>field!</c>.</summary>
public enum SortNulls
{
    /// <summary>No special null handling; the database default sort order applies.</summary>
    None,

    /// <summary>Sort non-null values first, then nulls (requested with <c>field?</c>).</summary>
    HasValue,

    /// <summary>Sort null values first, then non-nulls (requested with <c>field!</c>).</summary>
    NotHasValue,
}