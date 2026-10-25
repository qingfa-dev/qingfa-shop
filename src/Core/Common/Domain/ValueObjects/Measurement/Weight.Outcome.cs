namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Weight"/> creation and arithmetic.</summary>
public static class WeightOutcome
{
    /// <summary>Failure error constants for <see cref="Weight"/> validation and operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the weight amount is negative.</summary>
        public static Error ValueNegative => Error.BadRequest(
            code: "Weight.Value.Negative",
            message: "Weight must not be negative.");

        /// <summary>Error returned when the unit code is invalid.</summary>
        public static Error UnitInvalid => Error.BadRequest(
            code: "Weight.Unit.Invalid",
            message: "Weight unit must be a valid unit code.");

        /// <summary>Error returned when arithmetic operands use different units.</summary>
        public static Error UnitMismatch => Error.BadRequest(
            code: "Weight.Unit.Mismatch",
            message: "Units must match for this operation.");

        #endregion
    }
}