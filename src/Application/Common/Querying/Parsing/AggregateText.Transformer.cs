using System.Text.RegularExpressions;

using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Mapping;

namespace Application.Common.Querying.Parsing;

/// <summary>Parses aggregate expressions into neutral aggregate columns.</summary>
internal static partial class AggregateText
{
    [GeneratedRegex(QueryConstant.Functions.AggregatePattern, RegexOptions.IgnoreCase)]
    private static partial Regex AggregatePattern();

    /// <summary>Parses a comma-separated aggregate expression into aggregate specs.</summary>
    /// <param name="text">The raw aggregate text (e.g., "count(Id),sum(Price)").</param>
    /// <param name="state">The parser state with the query map and limits.</param>
    /// <returns>A list of aggregate specs, or a failure if syntax is invalid.</returns>
    public static Result<IReadOnlyList<AggregateSpec>> Parse<T>(string? text, ParserState<T> state)
    {
        // Default: no aggregate text means no aggregates.
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<AggregateSpec>>.Success([]);

        // Split: each comma-separated part is one aggregate expression.
        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Guard: limit the number of aggregates to prevent excessive SQL functions.
        if (parts.Length > state.Options.MaxAggregates)
        {
            return Result<IReadOnlyList<AggregateSpec>>.Fail(
                QueryOutcome.Failure.AggregateSyntax(
                    $"cannot exceed {state.Options.MaxAggregates} aggregates"));
        }

        var errors = new List<Error>();
        var result = new List<AggregateSpec>();
        foreach (string part in parts)
        {
            // Parse: the regex extracts the function name and field from "function(field)" syntax.
            Match match = AggregatePattern().Match(part);
            if (!match.Success)
            {
                errors.Add(QueryOutcome.Failure.AggregateSyntax(part));
                continue;
            }

            // Normalize: the function name is matched case-insensitively and kept
            // lower-cased, so `distinctCount` and `distinctcount` are the same
            // aggregate and produce the same column name.
            string fn = match.Groups["fn"].Value.ToLowerInvariant();
            string fieldName = match.Groups["field"].Value;

            // Resolve: look up the field in the map.
            Result<QueryField> mapped = state.Field(fieldName);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            // Map: convert the function name string to the enum value.
            AggregateFunction function = fn switch
            {
                QueryConstant.Functions.Sum => AggregateFunction.Sum,
                QueryConstant.Functions.Average => AggregateFunction.Average,
                QueryConstant.Functions.Minimum => AggregateFunction.Minimum,
                QueryConstant.Functions.Maximum => AggregateFunction.Maximum,
                QueryConstant.Functions.Count => AggregateFunction.Count,
                QueryConstant.Functions.DistinctCount => AggregateFunction.DistinctCount,
                // Fallback: unknown function names default to Count.
                _ => AggregateFunction.Count,
            };

            // Validate: an aggregate the field's type cannot support is rejected
            // here, before any provider work.
            if (!IsSupported(function, field.ComparisonType))
            {
                errors.Add(QueryOutcome.Failure.AggregateUnsupported(fieldName));
                continue;
            }

            result.Add(new AggregateSpec(function, fieldName, $"{fn}({fieldName})"));
        }

        // Collect: accumulate all errors so the caller sees every problem in one pass.
        return errors.Count > 0
            ? Result<IReadOnlyList<AggregateSpec>>.Fail(errors)
            : Result<IReadOnlyList<AggregateSpec>>.Success(result);
    }

    /// <summary>
    /// Checks whether an aggregate function is supported for the given field type.
    /// </summary>
    /// <param name="function">The aggregate function to check.</param>
    /// <param name="type">The field's .NET type.</param>
    /// <returns>True when the function can operate on the type.</returns>
    private static bool IsSupported(AggregateFunction function, Type type)
    {
        // Unwrap: Nullable<T> exposes the underlying type for the type check.
        Type target = Nullable.GetUnderlyingType(type) ?? type;

        return function switch
        {
            // Validate: Sum and Average require numeric types — they cannot operate on strings or dates.
            AggregateFunction.Sum or AggregateFunction.Average => target == typeof(byte)
                || target == typeof(sbyte) || target == typeof(short) || target == typeof(ushort)
                || target == typeof(int) || target == typeof(uint) || target == typeof(long)
                || target == typeof(ulong) || target == typeof(float) || target == typeof(double)
                || target == typeof(decimal),
            // Validate: Min and Max require IComparable — any ordered type (string, date, numeric).
            AggregateFunction.Minimum or AggregateFunction.Maximum =>
                typeof(IComparable).IsAssignableFrom(target),
            // Count and DistinctCount work on any type — no type constraint needed.
            _ => true,
        };
    }
}