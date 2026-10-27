namespace Application.Common.Querying.Sorting;

/// <summary>The direction of a sort field.</summary>
public enum SortDirection
{
    /// <summary>Sort from smallest to largest (A-Z, 0-9, oldest-first).</summary>
    Ascending,

    /// <summary>Sort from largest to smallest (Z-A, 9-0, newest-first).</summary>
    Descending,
}