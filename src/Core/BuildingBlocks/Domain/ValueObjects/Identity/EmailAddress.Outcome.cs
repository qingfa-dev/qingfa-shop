namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>Result constants for the <see cref="EmailAddress"/> value object.</summary>
public static class EmailAddressOutcome
{
    /// <summary>Failure error constants for the <see cref="EmailAddress"/> value object.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the address is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: Codes.ValueRequired,
            message: "Email address must be specified.");

        /// <summary>Error returned when the address exceeds its maximum length.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: Codes.ValueTooLong,
            message:
                $"Email address cannot exceed {EmailAddressConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error returned when the address does not match the configured syntax.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: Codes.ValueInvalid,
            message: "Email address format is invalid.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ValueRequired = "EmailAddress.Value.Required";
        public const string ValueTooLong = "EmailAddress.Value.TooLong";
        public const string ValueInvalid = "EmailAddress.Value.Invalid";
    }
}