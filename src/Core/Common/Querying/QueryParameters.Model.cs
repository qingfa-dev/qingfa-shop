namespace Core.Common.Querying;

/// <summary>The query-string shape accepted by the parser. No ASP.NET type appears here.</summary>
public sealed record QueryParameters
{
    public string? Filter { get; init; }
    public string? OrderBy { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Search { get; init; }
    public string? Select { get; init; }
    public string? GroupBy { get; init; }
    public string? Aggregate { get; init; }
}