namespace Core.Common.Domain.ValueObjects.Times;

/// <summary>Provides inclusive date-range calculations.</summary>
public static class DateRangeExtensions
{
    /// <summary>
    /// Total days covered by the range, counting both ends.
    /// </summary>
    /// <summary>Returns the number of calendar days covered, including both endpoints.</summary>
    /// <param name="range">The inclusive date range.</param>
    /// <returns>The inclusive day count.</returns>
    public static int Duration(this DateRange range)
    {
        return range.End.DayNumber - range.Start.DayNumber + 1;
    }

    /// <summary>
    /// Whether the given date falls inside the range (inclusive).
    /// </summary>
    /// <summary>Determines whether a date is inside the range, including either endpoint.</summary>
    /// <param name="range">The inclusive date range.</param>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true"/> when the date is included.</returns>
    public static bool Includes(this DateRange range, DateOnly date)
    {
        return date >= range.Start && date <= range.End;
    }

    /// <summary>
    /// Whether two ranges share at least one day.
    /// </summary>
    /// <summary>Determines whether two inclusive ranges share at least one date.</summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <returns><see langword="true"/> when the ranges overlap.</returns>
    public static bool Overlaps(
        this DateRange left,
        DateRange right)
    {
        return left.Start <= right.End && right.Start <= left.End;
    }
}