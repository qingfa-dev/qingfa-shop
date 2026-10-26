namespace Core.Common.Domain.ValueObjects.Times;

/// <summary>An inclusive range of calendar dates.</summary>
/// <param name="Start">The first included date.</param>
/// <param name="End">The last included date.</param>
public readonly partial record struct DateRange(DateOnly Start, DateOnly End)
{
    /// <summary>Creates a date range whose end is not before its start.</summary>
    /// <param name="start">The first included date.</param>
    /// <param name="end">The last included date.</param>
    /// <returns>The range, or a validation error when the dates are reversed.</returns>
    public static Result<DateRange> Create(
        DateOnly start,
        DateOnly end)
    {
        var candidate = new DateRange(start, end);

        return DateRangeValidator.Validate(candidate);
    }
}