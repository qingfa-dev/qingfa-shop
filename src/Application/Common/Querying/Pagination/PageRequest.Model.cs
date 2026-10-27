namespace Application.Common.Querying.Pagination;

/// <summary>A validated offset page request.</summary>
/// <param name="Page">1-based page number (1 = first page).</param>
/// <param name="PageSize">Number of items per page (must be between 1 and the configured maximum).</param>
public sealed record PageRequest(
    int Page,
    int PageSize);