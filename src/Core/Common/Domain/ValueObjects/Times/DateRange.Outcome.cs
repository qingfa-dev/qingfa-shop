namespace Core.Common.Domain.ValueObjects.Times;

/// <summary>Groups validation errors for <see cref="DateRange"/> creation.</summary>
public static class DateRangeOutcome
{
    /// <summary>Contains date-range validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the end date precedes the start date.</summary>
        public static Error RangeInvalid => Error.BadRequest(
            code: "DateRange.Range.Invalid",
            message: "Start date must not be after end date.");

        #endregion
    }
}