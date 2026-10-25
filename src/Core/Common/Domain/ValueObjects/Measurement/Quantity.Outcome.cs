namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Quantity"/> creation and arithmetic.</summary>
public static class QuantityOutcome
{
    /// <summary>Failure error constants for <see cref="Quantity"/> validation and operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the amount is negative.</summary>
        public static Error ValueNegative => Error.BadRequest(
            code: "Quantity.Value.Negative",
            message: "Quantity must not be negative.");

        /// <summary>Error returned when the unit code is invalid.</summary>
        public static Error UnitInvalid => Error.BadRequest(
            code: "Quantity.Unit.Invalid",
            message: "Quantity unit must be a valid unit code.");

        /// <summary>Error returned when arithmetic operands use different units.</summary>
        public static Error UnitMismatch => Error.BadRequest(
            code: "Quantity.Unit.Mismatch",
            message: "Units must match for this operation.");

        #endregion
    }
}