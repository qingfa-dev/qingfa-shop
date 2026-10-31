namespace Application.BuildingBlocks.Querying.Aggregation;

/// <summary>Checks whether an aggregate function can operate on a value type.</summary>
public static class AggregateFunctionRule
{
    /// <summary>Returns true when the function supports the supplied type.</summary>
    /// <param name="function">The aggregate function to check.</param>
    /// <param name="type">The .NET type produced by the aggregated field.</param>
    /// <returns>True when the function can operate on the type.</returns>
    public static bool IsSupported(AggregateFunction function, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        // Unwrap: Nullable<T> exposes the underlying type for the type check.
        Type target = Nullable.GetUnderlyingType(type) ?? type;

        return function switch
        {
            // Validate: Sum and Average require numeric types — they cannot operate on strings or dates.
            AggregateFunction.Sum or AggregateFunction.Average =>
                target == typeof(byte) || target == typeof(sbyte) ||
                target == typeof(short) || target == typeof(ushort) ||
                target == typeof(int) || target == typeof(uint) ||
                target == typeof(long) || target == typeof(ulong) ||
                target == typeof(float) || target == typeof(double) ||
                target == typeof(decimal),
            // Validate: Min and Max require IComparable — any ordered type (string, date, numeric).
            AggregateFunction.Minimum or AggregateFunction.Maximum =>
                typeof(IComparable).IsAssignableFrom(target),
            // Count and DistinctCount work on any type — no type constraint needed.
            _ => true,
        };
    }
}