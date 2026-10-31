using Application.BuildingBlocks.Persistence.Capabilities;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Capabilities;

/// <summary>
/// EF Core + PostgreSQL query capabilities.
/// Reports what the provider can translate so callers fail early instead of
/// hitting provider exceptions at runtime.
/// </summary>
public sealed class EfCoreQueryCapabilities : IQueryCapabilities
{
    // ---- Shapes — EF Core + Npgsql supports all of these ---------------
    public bool SupportsExpressions => true;
    public bool SupportsGrouping => true;
    public bool SupportsAggregates => true;
    public bool SupportsJoins => true;
    public bool SupportsSubqueries => true;
    public bool SupportsDistinct => true;
    public bool SupportsUnion => true;
    public bool SupportsCaseInsensitive => true;
    public bool SupportsStreaming => true;
    public bool SupportsProjection => true;
    public bool SupportsChangeTracking => true;

    // ---- Search — EF Core supports basic string operations -------------
    /// <summary>
    /// EF Core supports Contains, StartsWith, and Exact (==) via LINQ translation.
    /// Full-text and fuzzy search require PostgreSQL extensions (pg_trgm, tsvector)
    /// and are not enabled by default.
    /// </summary>
    public SearchModeSupport SearchModes =>
        SearchModeSupport.Contains | SearchModeSupport.StartsWith | SearchModeSupport.Exact;

    public bool SupportsRanking => false;
    public bool SupportsHighlighting => false;
    public int? MaxSearchTerms => QueryCapabilityConstant.Search.MaxTerms;
    public int? MaxSearchFields => QueryCapabilityConstant.Search.MaxFields;

    // ---- Limits --------------------------------------------------------
    /// <summary>EF Core has no hard page-size limit; use a sensible default.</summary>
    public int? MaxPageSize => QueryCapabilityConstant.Limits.MaxPageSize;

    /// <summary>EF Core has no hard expression depth limit.</summary>
    public int? MaxExpressionDepth => null;

    /// <summary>EF Core has no hard order-by clause limit.</summary>
    public int? MaxOrderByClauses => null;
}
