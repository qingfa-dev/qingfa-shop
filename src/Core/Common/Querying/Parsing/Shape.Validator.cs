using Core.Common.Querying.Aggregation;
using Core.Common.Querying.Grouping;
using Core.Common.Querying.Projection;

namespace Core.Common.Querying.Parsing;

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
        if (select is not null && aggregate is not null && group is null)
            return Result.Fail(QueryOutcome.Failure.MixedShape);

        if (group is null || select is null)
            return Result.Success();

        foreach (SelectField field in select)
        {
            if (!group.Any(key => string.Equals(key.Field, field.Field, StringComparison.OrdinalIgnoreCase)))
                return Result.Fail(QueryOutcome.Failure.FieldNotGrouped(field.Field));
        }

        return Result.Success();
    }
}