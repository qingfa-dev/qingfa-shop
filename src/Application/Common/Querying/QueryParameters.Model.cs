
using Application.Common.Querying.Searching;

namespace Application.Common.Querying;
/// <summary>The query-string shape accepted by the parser. No ASP.NET type appears here.</summary>
public sealed record QueryParameters
{
    /// <summary>Raw filter expression text (e.g., "Status=active,Name=*john").</summary>
    public string? Filter { get; init; }

    /// <summary>Raw ordering expression text (e.g., "Name,-CreatedAt").</summary>
    public string? OrderBy { get; init; }

    /// <summary>1-based page number; null means no pagination.</summary>
    public int? Page { get; init; }

    /// <summary>Number of items per page; null means use the default page size.</summary>
    public int? PageSize { get; init; }

    /// <summary>Raw search terms text (e.g., "hello world").</summary>
    public string? Search { get; init; }

    /// <summary>Comma-separated list of field names to restrict search to; null means search all searchable fields.</summary>
    public string? SearchFields { get; init; }

    /// <summary>The search algorithm (contains, startswith, exact, fulltext, fuzzy).</summary>
    public SearchMode? SearchMode { get; init; }

    /// <summary>Whether multiple search terms are combined with AND or OR logic.</summary>
    public SearchTermLogic? SearchTermLogic { get; init; }

    /// <summary>Whether the search should ignore case; null means use the provider default.</summary>
    public bool? SearchCaseInsensitive { get; init; }

    /// <summary>Raw projection expression text (e.g., "Name,CreatedAt as Date").</summary>
    public string? Select { get; init; }

    /// <summary>Raw group-by expression text (e.g., "Status,Category").</summary>
    public string? GroupBy { get; init; }

    /// <summary>Raw aggregate expression text (e.g., "count(Id),sum(Price)").</summary>
    public string? Aggregate { get; init; }
}