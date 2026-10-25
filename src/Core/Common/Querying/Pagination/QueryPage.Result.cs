namespace Core.Common.Querying.Pagination;

/// <summary>One page of entities plus the total count before paging.</summary>
public sealed record QueryPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    /// <summary>Gets the number of pages at the requested size.</summary>
    public long TotalPages => PageSize <= 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

    /// <summary>Gets a value indicating whether a previous page exists.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>Gets a value indicating whether a next page exists.</summary>
    public bool HasNext => Page < TotalPages;
}