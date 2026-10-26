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
    public BasicSearchRequest(string query, SearchMode mode) : base(query, mode)
    {
        if (mode == SearchMode.FullText)
            throw new ArgumentException("Use FullTextSearchRequest for FullText mode.", nameof(mode));
    }
}

/// <summary>Full-text search request with ranking and language options.</summary>
public sealed record FullTextSearchRequest : SearchRequest
{
    /// <summary>Creates a full-text search request.</summary>
    /// <param name="query">The text to search for.</param>
    /// <param name="language">The language for stemming/stopwords (e.g. "en", "de").</param>
    /// <param name="includeRank">Whether to include relevance score in results.</param>
    /// <param name="minScore">Minimum relevance score (0-1) to include in results.</param>
    public FullTextSearchRequest(
        string query,
        string? language = null,
        bool includeRank = false,
        double? minScore = null)
        : base(query, SearchMode.FullText)
    {
        if (language != null && language.Length > QueryConstant.Limits.Search.MaxLanguageLength)
            throw new ArgumentException(
                $"Language code cannot exceed {QueryConstant.Limits.Search.MaxLanguageLength} characters.", nameof(language));

        if (minScore.HasValue && (minScore < 0 || minScore > 1))
            throw new ArgumentException("Minimum score must be between 0 and 1.", nameof(minScore));

        Language = language;
        IncludeRank = includeRank;
        MinScore = minScore;
    }

    /// <summary>Language for stemming and stopwords (e.g. "en", "de").</summary>
    public string? Language { get; init; }

    /// <summary>Whether to include relevance score in results (e.g. SQL Server RANK, PostgreSQL ts_rank).</summary>
    public bool IncludeRank { get; init; }

    /// <summary>Minimum relevance score (0-1) to include in results.</summary>
    public double? MinScore { get; init; }
}