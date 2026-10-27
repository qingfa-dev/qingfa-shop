namespace Contract.Common;

/// <summary>One page of models plus the total count before paging.</summary>
/// <typeparam name="T">The model type in the page.</typeparam>
/// <param name="Items">The models on the current page.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The requested page size.</param>
/// <param name="TotalCount">The total number of rows before paging.</param>
public sealed record PageModel<T>(
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
