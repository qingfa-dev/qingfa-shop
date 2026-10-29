namespace Application.Common.Querying.Aggregation;

/// <summary>Maps aggregate functions to the tokens the query grammar accepts.</summary>
public static class AggregateFunctionExtension
{
    /// <summary>Returns the canonical grammar token for an aggregate function.</summary>
    /// <param name="function">The aggregate function to translate.</param>
    /// <returns>The lower-case token used by the text parser and default aliases.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The function is not a known <see cref="AggregateFunction"/> value.
    /// </exception>
    public static string ToToken(this AggregateFunction function) => function switch
    {
        AggregateFunction.Sum => QueryConstant.Functions.Sum,
        AggregateFunction.Average => QueryConstant.Functions.Average,
        AggregateFunction.Minimum => QueryConstant.Functions.Minimum,
        AggregateFunction.Maximum => QueryConstant.Functions.Maximum,
        AggregateFunction.Count => QueryConstant.Functions.Count,
        AggregateFunction.DistinctCount => QueryConstant.Functions.DistinctCount,
        // Guard: An unmapped enum value would emit a column name no parser can produce.
        _ => throw new ArgumentOutOfRangeException(
            nameof(function), function, "Unknown aggregate function."),
    };
}