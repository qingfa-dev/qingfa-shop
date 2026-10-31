using Application.BuildingBlocks.Querying.Pagination;

namespace Application.BuildingBlocks.Querying.Parsing;

/// <summary>Resolves and validates page number and size against the configured limits.</summary>
internal static class PageText
{
    /// <summary>Parses and validates page number and size against the configured limits.</summary>
    /// <param name="page">The requested page number (1-based), or null for the default.</param>
    /// <param name="pageSize">The requested page size, or null for the default.</param>
    /// <param name="state">The parser state with the configured limits.</param>
    /// <returns>A validated page request, or a failure if values are out of range.</returns>
    public static Result<PageRequest> Parse<T>(int? page, int? pageSize, ParserState<T> state)
    {
        // Default: page defaults to 1 (first page); size falls back to the configured default.
        int resolvedPage = page ?? 1;
        int resolvedSize = pageSize ?? state.Options.DefaultPageSize;

        // Validate: paging is strict. An out-of-range value is an error, never a
        // silent clamp, so the caller learns the request was wrong.
        if (resolvedPage < 1)
            return Result<PageRequest>.Fail(QueryOutcome.Failure.PageInvalid);

        // Validate: page size must be within the configured bounds.
        if (resolvedSize < 1 || resolvedSize > state.Options.MaxPageSize)
            return Result<PageRequest>.Fail(
                QueryOutcome.Failure.PageSizeInvalid(state.Options.MaxPageSize));

        return Result<PageRequest>.Success(new PageRequest(resolvedPage, resolvedSize));
    }
}