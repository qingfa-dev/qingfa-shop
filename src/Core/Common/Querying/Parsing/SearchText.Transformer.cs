namespace Core.Common.Querying.Parsing;

/// <summary>Trims and validates free-text search against configured limits and searchable fields.</summary>
internal static class SearchText
{
    /// <summary>Parses the search text.</summary>
    /// <param name="text">The raw search text.</param>
    /// <param name="state">The shared parser state.</param>
    /// <returns>
    /// The trimmed search text, or an empty string when no search was requested.
    /// A failure describes a search the map cannot serve.
    /// </returns>
    public static Result<string> Parse<T>(string? text, ParserState<T> state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<string>.Success(string.Empty);

        if (text.Length > state.Options.MaxSearchLength)
        {
            return Result<string>.Fail(
                QueryOutcome.Failure.SearchTooLong(state.Options.MaxSearchLength));
        }

        // Validate: a map with no searchable field cannot serve a search, so the
        // request is rejected instead of silently matching nothing.
        if (!state.Map.Fields.Any(field => field.Searchable))
            return Result<string>.Fail(QueryOutcome.Failure.SearchNotConfigured);

        return Result<string>.Success(text.Trim());
    }
}