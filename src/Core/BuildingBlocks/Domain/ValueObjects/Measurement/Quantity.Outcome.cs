namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Quantity"/> creation and arithmetic.</summary>
public static class QuantityOutcome
{
    /// <summary>Failure error constants for <see cref="Quantity"/> validation and operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the amount is negative.</summary>
        public static Error ValueNegative => Error.BadRequest(
            code: Codes.ValueNegative,
            message: "Quantity must not be negative.");

        /// <summary>Error returned when the unit code is invalid.</summary>
        public static Error UnitInvalid => Error.BadRequest(
            code: Codes.UnitInvalid,
            message: "Quantity unit must be a valid unit code.");

        /// <summary>Error returned when arithmetic operands use different units.</summary>
        public static Error UnitMismatch => Error.BadRequest(
            code: Codes.UnitMismatch,
            message: "Units must match for this operation.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ValueNegative = "Quantity.Value.Negative";
        public const string UnitInvalid = "Quantity.Unit.Invalid";
        public const string UnitMismatch = "Quantity.Unit.Mismatch";
    }
}