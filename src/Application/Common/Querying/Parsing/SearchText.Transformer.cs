
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Searching;

namespace Application.Common.Querying.Parsing;
/// <summary>Trims and validates free-text search against configured limits and searchable fields.</summary>
internal static class SearchText
{
    /// <summary>Parses the search parameters into a SearchRequest.</summary>
    /// <param name="parameters">The query parameters containing search options.</param>
    /// <param name="state">The shared parser state.</param>
    /// <returns>
    /// A SearchRequest when search was requested.
    /// A failure describes a search the map cannot serve.
    /// </returns>
    public static Result<SearchRequest> Parse<T>(QueryParameters parameters, ParserState<T> state)
    {
        string query = parameters.Search!.Trim();

        // Guard: reject oversized search text before processing.
        if (query.Length > state.Options.MaxSearchLength)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.SearchTooLong(state.Options.MaxSearchLength));
        }

        // Validate: a map with no searchable field cannot serve a search, so the
        // request is rejected instead of silently matching nothing.
        if (!state.Map.Fields.Any(field => field.Searchable))
            return Result<SearchRequest>.Fail(QueryOutcome.Failure.SearchNotConfigured);

        // Parse: optional field list restricts search to specific columns.
        IReadOnlyList<string>? fields = null;
        if (!string.IsNullOrWhiteSpace(parameters.SearchFields))
        {
            fields = parameters.SearchFields
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // Validate: each named field must exist in the map and be marked searchable,
            // so a typo or a non-searchable field fails fast rather than silently being ignored.
            foreach (string fieldName in fields)
            {
                var fieldResult = state.Map.TryGet(fieldName, out QueryField? field);
                if (!fieldResult)
                    return Result<SearchRequest>.Fail(QueryOutcome.Failure.NotMapped(fieldName));
                if (!field!.Searchable)
                    return Result<SearchRequest>.Fail(QueryOutcome.Failure.SearchNotConfigured);
            }
        }

        // Split: whitespace separates search terms; the count is limited to prevent
        // the adapter from generating an excessively large WHERE clause.
        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length > state.Options.MaxSearchTerms)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.TooManySearchTerms(state.Options.MaxSearchTerms));
        }

        // Validate: limit the number of target fields to keep the query narrow.
        if (fields != null && fields.Count > state.Options.MaxSearchFields)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.TooManySearchFields(state.Options.MaxSearchFields));
        }

        // Default: Contains is the most common search mode — partial substring match.
        var mode = parameters.SearchMode ?? SearchMode.Contains;

        // Select: FullText uses a specialized request type; all other modes use BasicSearchRequest.
        SearchRequest request = mode switch
        {
            SearchMode.FullText => new FullTextSearchRequest(query)
            {
                Fields = fields,
                TermLogic = parameters.SearchTermLogic ?? SearchTermLogic.And,
                CaseInsensitive = parameters.SearchCaseInsensitive ?? true,
            },
            _ => new BasicSearchRequest(query, mode)
            {
                Fields = fields,
                TermLogic = parameters.SearchTermLogic ?? SearchTermLogic.And,
                CaseInsensitive = parameters.SearchCaseInsensitive ?? true,
            }
        };

        return Result<SearchRequest>.Success(request);
    }
}