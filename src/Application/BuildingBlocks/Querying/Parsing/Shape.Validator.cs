using Application.BuildingBlocks.Querying.Aggregation;
using Application.BuildingBlocks.Querying.Grouping;
using Application.BuildingBlocks.Querying.Projection;

namespace Application.BuildingBlocks.Querying.Parsing;

/// <summary>Checks that select, group, and aggregate combine into a valid tabular shape.</summary>
internal static class ShapeValidator
{
    /// <summary>Validates the combination of the tabular parts.</summary>
    /// <param name="select">The selected columns, or null when none were requested.</param>
    /// <param name="group">The grouping keys, or null when none were requested.</param>
    /// <param name="aggregate">The aggregates, or null when none were requested.</param>
    /// <returns>Success when the shape is valid; otherwise a failure.</returns>
    public static Result Validate(
        IReadOnlyList<SelectField>? select,
        IReadOnlyList<GroupByField>? group,
        IReadOnlyList<AggregateSpec>? aggregate)
    {
        // Enforce: in SQL, mixing SELECT columns with aggregates requires GROUP BY.
        // Without it, the query shape is ambiguous — the adapter cannot produce a valid result.
        if (select is not null && aggregate is not null && group is null)
            return Result.Fail(QueryOutcome.Failure.MixedShape);

        // Skip: no group-by means no select+group validation is needed.
        if (group is null || select is null)
            return Result.Success();

        // Validate: every selected field must appear in the group-by keys,
        // mirroring the SQL rule that non-aggregated columns must be grouped.
        foreach (SelectField field in select)
        {
            if (!group.Any(key => string.Equals(key.Field, field.Field, StringComparison.OrdinalIgnoreCase)))
                return Result.Fail(QueryOutcome.Failure.FieldNotGrouped(field.Field));
        }

        return Result.Success();
    }
}