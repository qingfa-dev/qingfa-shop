namespace Application.Common.Querying.Aggregation;

/// <summary>The aggregate applied to a field.</summary>
public enum AggregateFunction
{
    Sum,
    Average,
    Minimum,
    Maximum,
    Count,
    DistinctCount,
}