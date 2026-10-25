namespace Core.Common.Domain.ValueObjects.Times;

/// <summary>Groups validation errors for <see cref="TimeRange"/> creation.</summary>
public static class TimeRangeOutcome
{
    /// <summary>Contains time-range validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the end time precedes the start time.</summary>
        public static Error RangeInvalid => Error.BadRequest(
            code: "TimeRange.Range.Invalid",
            message: "Start time must not be after end time.");

        #endregion
    }
}