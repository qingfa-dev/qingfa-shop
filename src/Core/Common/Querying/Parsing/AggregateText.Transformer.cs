using System.Text.RegularExpressions;

using Core.Common.Querying.Aggregation;
using Core.Common.Querying.Mapping;

namespace Core.Common.Querying.Parsing;

/// <summary>Parses aggregate expressions into neutral aggregate columns.</summary>
internal static partial class AggregateText
{
    [GeneratedRegex(QueryConstant.Functions.AggregatePattern, RegexOptions.IgnoreCase)]
    private static partial Regex AggregatePattern();

    public static Result<IReadOnlyList<AggregateSpec>> Parse<T>(string? text, ParserState<T> state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<IReadOnlyList<AggregateSpec>>.Success([]);

        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

            Result<QueryField> mapped = state.Field(fieldName);
            if (mapped.IsFailure)
            {
                errors.AddRange(mapped.Errors);
                continue;
            }

            QueryField field = mapped.Value;
            AggregateFunction function = fn switch
            {
                QueryConstant.Functions.Sum => AggregateFunction.Sum,
                QueryConstant.Functions.Average => AggregateFunction.Average,
                QueryConstant.Functions.Minimum => AggregateFunction.Minimum,
                QueryConstant.Functions.Maximum => AggregateFunction.Maximum,
                QueryConstant.Functions.Count => AggregateFunction.Count,
                QueryConstant.Functions.DistinctCount => AggregateFunction.DistinctCount,
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

        return errors.Count > 0
            ? Result<IReadOnlyList<AggregateSpec>>.Fail(errors)
            : Result<IReadOnlyList<AggregateSpec>>.Success(result);
    }

    private static bool IsSupported(AggregateFunction function, Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;

        return function switch
        {
            AggregateFunction.Sum or AggregateFunction.Average => target == typeof(byte)
                || target == typeof(sbyte) || target == typeof(short) || target == typeof(ushort)
                || target == typeof(int) || target == typeof(uint) || target == typeof(long)
                || target == typeof(ulong) || target == typeof(float) || target == typeof(double)
                || target == typeof(decimal),
            AggregateFunction.Minimum or AggregateFunction.Maximum =>
                typeof(IComparable).IsAssignableFrom(target),
            _ => true,
        };
    }
}