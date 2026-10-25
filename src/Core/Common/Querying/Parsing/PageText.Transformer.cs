using Core.Common.Querying.Pagination;

namespace Core.Common.Querying.Parsing;

/// <summary>Resolves and validates page number and size against the configured limits.</summary>
internal static class PageText
{
    public static Result<PageRequest> Parse<T>(int? page, int? pageSize, ParserState<T> state)
    {
        int resolvedPage = page ?? 1;
        int resolvedSize = pageSize ?? state.Options.DefaultPageSize;

        // Validate: paging is strict. An out-of-range value is an error, never a
        // silent clamp, so the caller learns the request was wrong.
        if (resolvedPage < 1)
            return Result<PageRequest>.Fail(QueryOutcome.Failure.PageInvalid);

        if (resolvedSize < 1 || resolvedSize > state.Options.MaxPageSize)
            return Result<PageRequest>.Fail(
                QueryOutcome.Failure.PageSizeInvalid(state.Options.MaxPageSize));

        return Result<PageRequest>.Success(new PageRequest(resolvedPage, resolvedSize));
    }
}