namespace Application.Common.Querying.Pagination;

/// <summary>A validated offset page request.</summary>
public sealed record PageRequest(int Page, int PageSize);