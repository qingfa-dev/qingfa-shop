namespace Application.BuildingBlocks.Querying.Searching;


/// <summary>How a search matches the text.</summary>
public enum SearchMode
{
    /// <summary>Substring match (<c>LIKE '%term%'</c> on both providers). Unindexed; slow at scale.</summary>
    Contains,

    /// <summary>Prefix match (<c>LIKE 'term%'</c>). Index-friendly on both providers.</summary>
    StartsWith,

    /// <summary>Whole-value equality. Ignored term logic: the whole <see cref="SearchRequest.Query"/> is one value.</summary>
    Exact,

    /// <summary>Provider-native full-text (SQL Server <c>CONTAINS</c> / <c>FREETEXT</c>;</summary>
    FullText,

    /// <summary>Approximate match (trigram similarity on PostgreSQL; <c>DIFFERENCE</c> or
    /// <c>SOUNDEX</c> on SQL Server). Requires capability opt-in.</summary>
    Fuzzy,
}

/// <summary>How the terms of a <see cref="SearchRequest.Query"/> combine.</summary>
public enum SearchTermLogic
{
    /// <summary>All terms must appear (AND).</summary>
    And,

    /// <summary>Any term may appear (OR).</summary>
    Or,
}