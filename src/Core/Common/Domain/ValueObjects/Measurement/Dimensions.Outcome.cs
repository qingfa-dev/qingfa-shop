namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Dimensions"/> creation.</summary>
public static class DimensionsOutcome
{
    /// <summary>Failure error constants for <see cref="Dimensions"/> creation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when length is negative.</summary>
        public static Error LengthNegative => Error.BadRequest(
            code: "Dimensions.Length.Negative",
            message: "Length must not be negative.");

        /// <summary>Error returned when width is negative.</summary>
        public static Error WidthNegative => Error.BadRequest(
            code: "Dimensions.Width.Negative",
            message: "Width must not be negative.");

        /// <summary>Error returned when height is negative.</summary>
        public static Error HeightNegative => Error.BadRequest(
            code: "Dimensions.Height.Negative",
            message: "Height must not be negative.");

        /// <summary>Error returned when the unit code is invalid.</summary>
        public static Error UnitInvalid => Error.BadRequest(
            code: "Dimensions.Unit.Invalid",
            message: "Dimensions unit must be a valid unit code.");

        #endregion
    }
}