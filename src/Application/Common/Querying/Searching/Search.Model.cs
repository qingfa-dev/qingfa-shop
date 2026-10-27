namespace Application.Common.Querying.Searching;

/// <summary>Abstract base for a textual search. Mirrors <see cref="Filtering.FilterNode"/>: provider-neutral, extensible.</summary>
/// <remarks>
/// <para>
/// Combined with <see cref="Query{T}.Filter"/> using AND. Both must match.
/// </para>
/// <para>
/// Subclasses encode mode-specific options (e.g. <c>Language</c>, <c>MinScore</c> for full-text).
/// </para>
/// </remarks>
public abstract record SearchRequest
{
    /// <summary>
    /// Creates a search request, validating that the query is not empty and does not exceed the maximum length.
    /// </summary>
    /// <param name="query">The text to search for; must not be null, empty, or exceed the configured limit.</param>
    /// <param name="mode">The search algorithm (Contains, StartsWith, Exact, FullText, Fuzzy).</param>
    protected SearchRequest(string query, SearchMode mode)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required.", nameof(query));
        if (query.Length > QueryConstant.Limits.Search.MaxLength)
            throw new ArgumentException(
                $"Search query exceeds {QueryConstant.Limits.Search.MaxLength} characters.", nameof(query));

        Query = query;
        Mode = mode;
    }

    /// <summary>The text to search for. May contain multiple terms; how they combine is
    /// controlled by <see cref="TermLogic"/>.</summary>
    public string Query { get; }

    /// <summary>Fields to search. <c>null</c> means "every field the adapter's map marks searchable".</summary>
    public IReadOnlyList<string>? Fields { get; init; }

    /// <summary>How the text is matched.</summary>
    public SearchMode Mode { get; }

    /// <summary>How multiple terms combine. <c>And</c> means all terms must match.</summary>
    public SearchTermLogic TermLogic { get; init; } = SearchTermLogic.And;

    /// <summary>Case sensitivity of the match. Defaults to case-insensitive.</summary>
    public bool CaseInsensitive { get; init; } = true;

    /// <summary>Splits <see cref="Query"/> into terms using whitespace; no quoting or
    /// escaping is interpreted. An adapter that supports phrase queries may inspect
    /// <see cref="Query"/> directly instead.</summary>
    public IReadOnlyList<string> GetTerms() =>
        Query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Standard search request for non-full-text modes (Contains, StartsWith, Exact, Fuzzy).</summary>
public sealed record BasicSearchRequest : SearchRequest
{
    /// <summary>
    /// Creates a basic search request for non-full-text modes.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <param name="mode">The search mode; must not be FullText (use FullTextSearchRequest instead).</param>
    public BasicSearchRequest(string query, SearchMode mode) : base(query, mode)
    {
        if (mode == SearchMode.FullText)
            throw new ArgumentException("Use FullTextSearchRequest for FullText mode.", nameof(mode));
    }
}

/// <summary>Full-text search request.</summary>
public sealed record FullTextSearchRequest : SearchRequest
{
    /// <summary>Creates a full-text search request with the given query text.</summary>
    /// <param name="query">The full-text search query.</param>
    public FullTextSearchRequest(string query)
        : base(query, SearchMode.FullText)
    {
    }
}