namespace Application.Common.Persistence.Capabilities;

/// <summary>What a persistence adapter can translate.</summary>
/// <remarks>
/// A provider is the only place that knows whether a given query shape can be
/// served. Read stores consult this before building a query, so unsupported
/// shapes become neutral failures instead of provider exceptions.
/// <para>
/// Custom operators are deliberately not here: the adapter's operator registry
/// already owns that list and reports an unknown operator directly.
/// </para>
/// </remarks>
public interface IQueryCapabilities
{
    // ---- Shapes --------------------------------------------------------
    bool SupportsExpressions     { get; }
    bool SupportsGrouping        { get; }
    bool SupportsAggregates      { get; }
    bool SupportsJoins           { get; }
    bool SupportsSubqueries      { get; }
    bool SupportsDistinct        { get; }
    bool SupportsUnion           { get; }
    bool SupportsCaseInsensitive { get; }
    bool SupportsStreaming       { get; }
    bool SupportsProjection      { get; }
    bool SupportsChangeTracking  { get; }

    // ---- Search --------------------------------------------------------
    SearchModeSupport SearchModes        { get; }
    bool              SupportsRanking    { get; }
    bool              SupportsHighlighting { get; }
    int?              MaxSearchTerms     { get; }
    int?              MaxSearchFields    { get; }

    // ---- Limits --------------------------------------------------------
    int? MaxPageSize        { get; }
    int? MaxExpressionDepth { get; }
    int? MaxOrderByClauses  { get; }
}