namespace Application.Common.Querying.Grouping;

/// <summary>One grouping key.</summary>
/// <param name="Field">The mapped field name used as a grouping key.</param>
public sealed record GroupByField(
    string Field);