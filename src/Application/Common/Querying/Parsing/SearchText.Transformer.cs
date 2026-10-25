namespace Application.Common.Querying.Parsing;

using Application.Common.Querying.Mapping;
using Application.Common.Querying.Searching;

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

        if (query.Length > state.Options.MaxSearchLength)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.SearchTooLong(state.Options.MaxSearchLength));
        }

        // Validate: a map with no searchable field cannot serve a search, so the
        // request is rejected instead of silently matching nothing.
        if (!state.Map.Fields.Any(field => field.Searchable))
            return Result<SearchRequest>.Fail(QueryOutcome.Failure.SearchNotConfigured);

        // Validate search fields if specified
        IReadOnlyList<string>? fields = null;
        if (!string.IsNullOrWhiteSpace(parameters.SearchFields))
        {
            fields = parameters.SearchFields
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // Verify each field exists and is searchable
            foreach (string fieldName in fields)
            {
                var fieldResult = state.Map.TryGet(fieldName, out QueryField? field);
                if (!fieldResult)
                    return Result<SearchRequest>.Fail(QueryOutcome.Failure.NotMapped(fieldName));
                if (!field!.Searchable)
                    return Result<SearchRequest>.Fail(QueryOutcome.Failure.SearchNotConfigured);
            }
        }

        // Validate MaxSearchTerms
        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length > state.Options.MaxSearchTerms)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.TooManySearchTerms(state.Options.MaxSearchTerms));
        }

        // Validate MaxSearchFields
        if (fields != null && fields.Count > state.Options.MaxSearchFields)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.TooManySearchFields(state.Options.MaxSearchFields));
        }

        // Validate MaxSearchLanguage
        if (!string.IsNullOrWhiteSpace(parameters.SearchLanguage) &&
            parameters.SearchLanguage.Length > state.Options.MaxSearchLanguage)
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.SearchLanguageTooLong(state.Options.MaxSearchLanguage));
        }

        // Validate MinScore range
        if (parameters.SearchMinScore.HasValue &&
            (parameters.SearchMinScore < 0 || parameters.SearchMinScore > 1))
        {
            return Result<SearchRequest>.Fail(
                QueryOutcome.Failure.InvalidMinScore);
        }

        var mode = parameters.SearchMode ?? SearchMode.Contains;

        // TODO: Replace with FullTextSearchRequest for FullText mode (with Language, IncludeRank, MinScore)
        var request = new BasicSearchRequest(query, mode)
        {
            Fields = fields,
            TermLogic = parameters.SearchTermLogic ?? SearchTermLogic.And,
            CaseInsensitive = parameters.SearchCaseInsensitive ?? true,
        };

        return Result<SearchRequest>.Success(request);
    }
}