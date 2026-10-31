namespace Core.BuildingBlocks.Domain.ValueObjects.Address;

/// <summary>Groups validation errors for <see cref="PostalCode"/> creation.</summary>
public static class PostalCodeOutcome
{
    /// <summary>Contains postal-code validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the postal code is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: Codes.ValueRequired,
            message: "Postal code must be specified.");

        /// <summary>Error returned when the postal code exceeds ten characters.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: Codes.ValueTooLong,
            message:
                $"Postal code cannot exceed {PostalCodeConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the postal code contains unsupported characters.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: Codes.ValueInvalid,
            message:
                "Postal code may contain only letters, digits, spaces and hyphens.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ValueRequired = "PostalCode.Value.Required";
        public const string ValueTooLong = "PostalCode.Value.TooLong";
        public const string ValueInvalid = "PostalCode.Value.Invalid";
    }
}