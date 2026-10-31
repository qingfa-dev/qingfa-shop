namespace Core.BuildingBlocks.Domain.ValueObjects.Times;

/// <summary>An inclusive range between two times on the same day.</summary>
/// <param name="Start">The first included time.</param>
/// <param name="End">The last included time.</param>
public readonly partial record struct TimeRange(TimeOnly Start, TimeOnly End)
{
    /// <summary>Creates a time range whose end is not before its start.</summary>
    /// <param name="start">The first included time.</param>
    /// <param name="end">The last included time.</param>
    /// <returns>The range, or a validation error when the times are reversed.</returns>
    public static Result<TimeRange> Create(
        TimeOnly start,
        TimeOnly end)
    {
        var candidate = new TimeRange(start, end);

        return TimeRangeValidator.Validate(candidate);
    }
}