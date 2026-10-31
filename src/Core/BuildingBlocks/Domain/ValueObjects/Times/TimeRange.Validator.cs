namespace Core.BuildingBlocks.Domain.ValueObjects.Times;

/// <summary>Validates time ranges and provides inclusive range calculations.</summary>
public static class TimeRangeValidator
{
    /// <summary>
    /// Validates a time range without mutating it.
    /// </summary>
    /// <summary>Returns success when the start time is not after the end time.</summary>
    /// <param name="range">The range to validate.</param>
    /// <returns>The unchanged range on success, or its validation error.</returns>
    public static Result<TimeRange> Validate(TimeRange range)
    {
        return Result<TimeRange>.Success(range)
            .Ensure(
                predicate: _ => range.Start <= range.End,
                error: TimeRangeOutcome.Failure.RangeInvalid);
    }
}