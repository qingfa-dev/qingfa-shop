namespace Core.BuildingBlocks.Domain.ValueObjects.Percentage;

/// <summary>Groups validation errors for <see cref="Percentage"/> creation.</summary>
public static class PercentageOutcome
{
    /// <summary>Contains percentage validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the value is below zero or above one hundred.</summary>
        public static Error ValueOutOfRange => Error.BadRequest(
            code: Codes.ValueOutOfRange,
            message:
                $"Percentage must be between {PercentageConstant.Constraints.Value.MinValue} and {PercentageConstant.Constraints.Value.MaxValue}.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ValueOutOfRange = "Percentage.Value.OutOfRange";
    }
}