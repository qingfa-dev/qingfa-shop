namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Dimensions"/> creation.</summary>
public static class DimensionsOutcome
{
    /// <summary>Failure error constants for <see cref="Dimensions"/> creation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when length is negative.</summary>
        public static Error LengthNegative => Error.BadRequest(
            code: Codes.LengthNegative,
            message: "Length must not be negative.");

        /// <summary>Error returned when width is negative.</summary>
        public static Error WidthNegative => Error.BadRequest(
            code: Codes.WidthNegative,
            message: "Width must not be negative.");

        /// <summary>Error returned when height is negative.</summary>
        public static Error HeightNegative => Error.BadRequest(
            code: Codes.HeightNegative,
            message: "Height must not be negative.");

        /// <summary>Error returned when the unit code is invalid.</summary>
        public static Error UnitInvalid => Error.BadRequest(
            code: Codes.UnitInvalid,
            message: "Dimensions unit must be a valid unit code.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string LengthNegative = "Dimensions.Length.Negative";
        public const string WidthNegative = "Dimensions.Width.Negative";
        public const string HeightNegative = "Dimensions.Height.Negative";
        public const string UnitInvalid = "Dimensions.Unit.Invalid";
    }
}