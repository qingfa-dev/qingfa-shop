namespace Core.BuildingBlocks.Domain.ValueObjects.Times;

/// <summary>Groups validation errors for <see cref="TimeRange"/> creation.</summary>
public static class TimeRangeOutcome
{
    /// <summary>Contains time-range validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the end time precedes the start time.</summary>
        public static Error RangeInvalid => Error.BadRequest(
            code: Codes.RangeInvalid,
            message: "Start time must not be after end time.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string RangeInvalid = "TimeRange.Range.Invalid";
    }
}