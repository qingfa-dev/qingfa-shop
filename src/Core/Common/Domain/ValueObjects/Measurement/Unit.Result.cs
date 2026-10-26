namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Result constants for <see cref="Unit"/> creation.</summary>
public static class UnitResult
{
    /// <summary>Failure error constants for <see cref="Unit"/> creation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the unit code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: "Unit.Code.Required",
            message: "Unit code must be specified.");

        /// <summary>Error returned when the unit code exceeds four characters.</summary>
        public static Error CodeTooLong => Error.BadRequest(
            code: "Unit.Code.TooLong",
            message:
                $"Unit code cannot exceed {UnitConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the unit code contains invalid characters.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: "Unit.Code.Invalid",
            message: "Unit code must be 1-4 lowercase letters.");

        #endregion
    }
}