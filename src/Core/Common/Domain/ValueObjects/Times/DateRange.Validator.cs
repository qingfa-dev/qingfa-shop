namespace Core.Common.Domain.ValueObjects.Times;

/// <summary>Validates date ranges without modifying them.</summary>
public static class DateRangeValidator
{
    /// <summary>
    /// Validates a date range without mutating it.
    /// </summary>
    /// <summary>Returns success when the start date is not after the end date.</summary>
    /// <param name="range">The range to validate.</param>
    /// <returns>The unchanged range on success, or its validation error.</returns>
    public static Result<DateRange> Validate(DateRange range)
    {
        return Result<DateRange>.Success(range)
            .Ensure(
                predicate: _ => range.Start <= range.End,
                error: DateRangeOutcome.Failure.RangeInvalid);
    }
}