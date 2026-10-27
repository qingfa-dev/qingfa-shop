using System.Globalization;
using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Projection;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>
    /// Evaluates group and aggregate shapes over an already-filtered row set.
    /// Uses client-side evaluation because EF Core cannot translate complex group-by + aggregate
    /// combinations to SQL reliably.
    /// </summary>
    public Result<TabularResult> BuildTabularClientSide(
        IReadOnlyList<T> rows,
        Query<T> query,
        QueryMap<T> map)
    {
        // Route: no group-by means aggregate-only (single row); otherwise group and aggregate.
        if (query.GroupBy is null || !query.GroupBy.Any())
        {
            // Validate: select and aggregate cannot combine without grouping.
            if (query.Select is not null || query.Aggregate is null)
                return Result<TabularResult>.Fail(QueryOutcome.Failure.MixedShape);

            return BuildAggregateOnly(rows, query.Aggregate, map);
        }

        return BuildGroups(rows, query, map);
    }

    /// <summary>
    /// Computes aggregate functions over the entire row set without grouping,
    /// producing a single-row result with one column per aggregate.
    /// </summary>
    private static Result<TabularResult> BuildAggregateOnly(
        IReadOnlyList<T> rows,
        IReadOnlyList<AggregateSpec> aggregates,
        QueryMap<T> map)
    {
        var columns = new List<TabularColumn>(aggregates.Count);
        var values = new List<object?>(aggregates.Count);

        foreach (AggregateSpec spec in aggregates)
        {
            Result<QueryField> mapped = ResolveAggregatable(spec, map);
            if (mapped.IsFailure)
                return Result<TabularResult>.Fail(mapped.Errors);

            Result<object?> aggregated = Aggregate(rows, spec, CreateAccessor(mapped.Value));
            if (aggregated.IsFailure)
                return Result<TabularResult>.Fail(aggregated.Errors);

            columns.Add(new TabularColumn(spec.Alias, ColumnType(spec, mapped.Value)));
            values.Add(aggregated.Value);
        }

        // Result: a single row with one value per aggregate.
        return Result<TabularResult>.Success(
            new TabularResult(columns, [new TabularRow(values)]));
    }

    /// <summary>
    /// Groups rows by the specified keys and computes aggregates per group.
    /// Validates that selected fields are included in the group-by keys.
    /// </summary>
    private static Result<TabularResult> BuildGroups(
        IReadOnlyList<T> rows,
        Query<T> query,
        QueryMap<T> map)
    {
        IReadOnlyList<SelectField> selectedKeys = query.Select ?? [];

        // Validate: a selected field must also be a grouping key.
        foreach (SelectField selected in selectedKeys)
        {
            if (!query.GroupBy!.Any(key =>
                    string.Equals(key.Field, selected.Field, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<TabularResult>.Fail(
                    QueryOutcome.Failure.FieldNotGrouped(selected.Field));
            }
        }

        // Resolve: build accessors for each group key so we can extract values from rows.
        var keys = new List<(GroupByField Key, QueryField Field, Func<T, object?> Accessor)>();
        foreach (GroupByField key in query.GroupBy!)
        {
            Result<QueryField> mapped = ResolveGroupable(key.Field, map);
            if (mapped.IsFailure)
                return Result<TabularResult>.Fail(mapped.Errors);

            keys.Add((key, mapped.Value, CreateAccessor(mapped.Value)));
        }

        // Resolve: build accessors for each aggregate.
        var aggregates = new List<(AggregateSpec Spec, QueryField Field, Func<T, object?> Accessor)>();
        foreach (AggregateSpec spec in query.Aggregate ?? Array.Empty<AggregateSpec>())
        {
            Result<QueryField> mapped = ResolveAggregatable(spec, map);
            if (mapped.IsFailure)
                return Result<TabularResult>.Fail(mapped.Errors);

            aggregates.Add((spec, mapped.Value, CreateAccessor(mapped.Value)));
        }

        // Assemble: build the column metadata — group keys first, then aggregates.
        var columns = new List<TabularColumn>(keys.Count + aggregates.Count);
        foreach (var entry in keys)
        {
            if (IncludesKey(selectedKeys, entry.Key.Field))
                columns.Add(new TabularColumn(
                    KeyColumnName(selectedKeys, entry.Key.Field), entry.Field.ComparisonType));
        }

        foreach (var entry in aggregates)
            columns.Add(new TabularColumn(entry.Spec.Alias, ColumnType(entry.Spec, entry.Field)));

        // Group: materialize and group client-side because EF Core cannot translate complex aggregates.
        IEnumerable<IGrouping<object?[], T>> groups = rows.GroupBy(
            row => keys.Select(key => key.Accessor(row)).ToArray(),
            ObjectArrayComparer.Instance);

        // Build: construct one row per group with key values and aggregate results.
        var result = new List<TabularRow>();
        foreach (IGrouping<object?[], T> group in groups)
        {
            var values = new List<object?>(columns.Count);

            for (int index = 0; index < keys.Count; index++)
            {
                if (IncludesKey(selectedKeys, keys[index].Key.Field))
                    values.Add(group.Key[index]);
            }

            foreach (var entry in aggregates)
            {
                Result<object?> aggregated = Aggregate(group, entry.Spec, entry.Accessor);
                if (aggregated.IsFailure)
                    return Result<TabularResult>.Fail(aggregated.Errors);

                values.Add(aggregated.Value);
            }

            result.Add(new TabularRow(values));
        }

        return Result<TabularResult>.Success(new TabularResult(columns, result));
    }

    /// <summary>
    /// Resolves the column name for a group-by key, honoring the caller's alias if provided.
    /// </summary>
    private static string KeyColumnName(IReadOnlyList<SelectField> selectedKeys, string field)
    {
        // Name: honor the alias the caller gave the key, as BuildSelectPlan does.
        foreach (SelectField selected in selectedKeys)
        {
            if (string.Equals(selected.Field, field, StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrEmpty(selected.Alias) ? field : selected.Alias;
        }

        // Default: no alias means the field name is the column name.
        return field;
    }

    // Check: if no select fields are specified, all keys are included.
    private static bool IncludesKey(IReadOnlyList<SelectField> selectedKeys, string field) =>
        selectedKeys.Count == 0 ||
        selectedKeys.Any(selected =>
            string.Equals(selected.Field, field, StringComparison.OrdinalIgnoreCase));

    // Catch: overflow and cast errors from numeric conversion are value problems, not faults.
    private static Result<object?> Aggregate(
        IEnumerable<T> rows,
        AggregateSpec spec,
        Func<T, object?> selector)
    {
        try
        {
            return AggregateCore(rows, spec, selector);
        }
        catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException)
        {
            // Translate: NaN, infinity, and decimal overflow are value problems, not faults.
            return Result<object?>.Fail(QueryOutcome.Failure.AggregateUnsupported(spec.Field));
        }
    }

    // Dispatch: compute the aggregate function over the row set.
    private static Result<object?> AggregateCore(
        IEnumerable<T> rows,
        AggregateSpec spec,
        Func<T, object?> selector)
    {
        // Precompute: evaluate all expensive aggregates once; the switch picks the result.
        object?[] values = rows.Select(selector).ToArray();

        object? avgResult = Average(values);
        object? minResult = Extreme(values, minimum: true);
        object? maxResult = Extreme(values, minimum: false);

        return spec.Function switch
        {
            AggregateFunction.Count => Result<object?>.Success(values.Length),
            AggregateFunction.DistinctCount => Result<object?>.Success(values.Distinct().Count()),
            AggregateFunction.Sum => Result<object?>.Success(Sum(values)),
            AggregateFunction.Average => avgResult is not null
                ? Result<object?>.Success(avgResult)
                : Result<object?>.Success(0m),
            AggregateFunction.Minimum => minResult is not null
                ? Result<object?>.Success(minResult)
                : Result<object?>.Success(0),
            AggregateFunction.Maximum => maxResult is not null
                ? Result<object?>.Success(maxResult)
                : Result<object?>.Success(0),
            _ => Result<object?>.Fail(QueryOutcome.Failure.AggregateUnsupported(spec.Field)),
        };
    }

    /// <summary>Converts non-null values to decimal and computes their sum.</summary>
    // Convert: filter nulls and sum as decimal using invariant culture.
    private static decimal Sum(object?[] values) => Numbers(values).Sum();

    // Compute: average of non-null numeric values, or null if empty.
    private static object? Average(object?[] values)
    {
        decimal[] numbers = Numbers(values);
        return numbers.Length == 0 ? null : numbers.Average();
    }

    /// <summary>
    /// Finds the minimum or maximum value using ordinal string comparison
    /// and IComparable for other types.
    /// </summary>
    // Find: minimum or maximum using ordinal string comparison and IComparable for other types.
    private static object? Extreme(object?[] values, bool minimum)
    {
        IComparable? best = null;
        foreach (IComparable candidate in values.OfType<IComparable>())
        {
            if (best is null)
            {
                best = candidate;
                continue;
            }

            // Order: strings compare ordinally so results do not depend on the host culture.
            int order = candidate is string left && best is string right
                ? string.CompareOrdinal(left, right)
                : candidate.CompareTo(best);

            if (minimum ? order < 0 : order > 0)
                best = candidate;
        }

        return best;
    }

    // Convert: filter nulls and convert to decimal using invariant culture for consistency.
    private static decimal[] Numbers(object?[] values) => values
        .Where(value => value is not null)
        .Select(value => Convert.ToDecimal(value, CultureInfo.InvariantCulture))
        .ToArray();

    /// <summary>
    /// Compiles a lambda expression that accesses a field and boxes the result to object.
    /// Used for heterogeneous projection where all values must be object.
    /// </summary>
    // Compile: create a delegate that accesses a field and boxes the result for heterogeneous projection.
    private static Func<T, object?> CreateAccessor(QueryField field)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "row");
        Expression body = Replace(field.Expression!, parameter);

        // Transform: wrap in object for heterogeneous projection.
        return Expression.Lambda<Func<T, object?>>(
            BuildSafeObject(body), parameter).Compile();
    }

    // Map: determine the result column type from the aggregate function.
    private static Type ColumnType(AggregateSpec spec, QueryField field) =>
        spec.Function switch
        {
            AggregateFunction.Count or AggregateFunction.DistinctCount => typeof(int),
            AggregateFunction.Sum or AggregateFunction.Average => typeof(decimal),
            AggregateFunction.Minimum or AggregateFunction.Maximum => field.ComparisonType,
            _ => typeof(object),
        };

    /// <summary>
    /// Checks whether an aggregate function is supported for a given type.
    /// Sum and Average require numeric types; Min and Max require IComparable.
    /// </summary>
    // Check: validate that the aggregate function supports the field's type.
    internal static bool Supports(AggregateFunction function, Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;

        return function switch
        {
            AggregateFunction.Sum or AggregateFunction.Average => IsNumeric(target),
            AggregateFunction.Minimum or AggregateFunction.Maximum =>
                typeof(IComparable).IsAssignableFrom(target),
            _ => true,
        };
    }

    // Check: numeric primitives that support Sum and Average.
    internal static bool IsNumeric(Type type) =>
        type == typeof(byte) ||
        type == typeof(sbyte) ||
        type == typeof(short) ||
        type == typeof(ushort) ||
        type == typeof(int) ||
        type == typeof(uint) ||
        type == typeof(long) ||
        type == typeof(ulong) ||
        type == typeof(float) ||
        type == typeof(double) ||
        type == typeof(decimal);
}