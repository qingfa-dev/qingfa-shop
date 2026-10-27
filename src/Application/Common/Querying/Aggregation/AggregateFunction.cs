namespace Application.Common.Querying.Aggregation;

/// <summary>The aggregate applied to a field.</summary>
public enum AggregateFunction
{
    /// <summary>Sum of all non-null values; requires a numeric field.</summary>
    Sum,

    /// <summary>Average of all non-null values; requires a numeric field.</summary>
    Average,

    /// <summary>Smallest non-null value; requires an ordered field (numeric, date, string).</summary>
    Minimum,

    /// <summary>Largest non-null value; requires an ordered field (numeric, date, string).</summary>
    Maximum,

    /// <summary>Count of all rows (including nulls) for the field.</summary>
    Count,

    /// <summary>Count of distinct non-null values for the field.</summary>
    DistinctCount,
}