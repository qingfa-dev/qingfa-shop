namespace Application.Common.Querying.Aggregation;

/// <summary>One aggregate column.</summary>
/// <param name="Function">The aggregate function to apply (Sum, Average, Count, etc.).</param>
/// <param name="Field">The mapped field name to aggregate over.</param>
/// <param name="Alias">The column alias in the result set (e.g., "TotalPrice", "ItemCount").</param>
public sealed record AggregateSpec(
    AggregateFunction Function,
    string Field,
    string Alias);