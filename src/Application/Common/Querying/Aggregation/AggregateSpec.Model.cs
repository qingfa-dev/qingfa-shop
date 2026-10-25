namespace Application.Common.Querying.Aggregation;

/// <summary>One aggregate column.</summary>
public sealed record AggregateSpec(
    AggregateFunction Function,
    string Field,
    string Alias);