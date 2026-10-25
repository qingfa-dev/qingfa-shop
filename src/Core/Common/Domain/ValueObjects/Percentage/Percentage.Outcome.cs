namespace Core.Common.Domain.ValueObjects.Percentage;

/// <summary>Groups validation errors for <see cref="Percentage"/> creation.</summary>
public static class PercentageOutcome
{
    /// <summary>Contains percentage validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the value is below zero or above one hundred.</summary>
        public static Error ValueOutOfRange => Error.BadRequest(
            code: "Percentage.Value.OutOfRange",
            message:
                $"Percentage must be between {PercentageConstant.Constraints.Value.MinValue} and {PercentageConstant.Constraints.Value.MaxValue}.");

        #endregion
    }
}