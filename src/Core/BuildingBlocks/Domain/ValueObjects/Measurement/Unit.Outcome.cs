namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Unit"/> creation.</summary>
public static class UnitOutcome
{
    /// <summary>Failure error constants for <see cref="Unit"/> creation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the unit code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: Codes.CodeRequired,
            message: "Unit code must be specified.");

        /// <summary>Error returned when the unit code exceeds four characters.</summary>
        public static Error CodeTooLong => Error.BadRequest(
            code: Codes.CodeTooLong,
            message:
                $"Unit code cannot exceed {UnitConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the unit code contains invalid characters.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: Codes.CodeInvalid,
            message: "Unit code must be 1-4 lowercase letters.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string CodeRequired = "Unit.Code.Required";
        public const string CodeTooLong = "Unit.Code.TooLong";
        public const string CodeInvalid = "Unit.Code.Invalid";
    }
}