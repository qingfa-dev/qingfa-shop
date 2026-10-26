namespace Core.Common.Domain.ValueObjects.Times;

/// <summary>Provides elapsed-time, containment, and overlap calculations for time ranges.</summary>
public static class TimeRangeExtensions
{
    /// <summary>Returns the elapsed time from the start to the end of the range.</summary>
    /// <param name="range">The time range.</param>
    /// <returns>The difference between its end and start times.</returns>
    public static TimeSpan Duration(this TimeRange range)
    {
        return range.End.ToTimeSpan() - range.Start.ToTimeSpan();
    }

    /// <summary>
    /// Whether the given time falls inside the range (inclusive).
    /// </summary>
    /// <summary>Determines whether a time is inside the range, including either endpoint.</summary>
    /// <param name="range">The inclusive time range.</param>
    /// <param name="time">The time to test.</param>
    /// <returns><see langword="true"/> when the time is included.</returns>
    public static bool Includes(this TimeRange range, TimeOnly time)
    {
        return time >= range.Start && time <= range.End;
    }

    /// <summary>
    /// Whether two ranges share at least one moment.
    /// </summary>
    /// <summary>Determines whether two inclusive ranges share at least one moment.</summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <returns><see langword="true"/> when the ranges overlap.</returns>
    public static bool Overlaps(
        this TimeRange left,
        TimeRange right)
    {
        return left.Start <= right.End && right.Start <= left.End;
    }
}