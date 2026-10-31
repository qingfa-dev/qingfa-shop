namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>Result constants for the <see cref="PhoneNumber"/> value object.</summary>
public static class PhoneNumberOutcome
{
    /// <summary>Failure error constants for the <see cref="PhoneNumber"/> value object.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the phone number is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: Codes.ValueRequired,
            message: "Phone number must be specified.");

        /// <summary>Error returned when the phone number exceeds its maximum length.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: Codes.ValueTooLong,
            message:
                $"Phone number cannot exceed {PhoneNumberConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error returned when the phone number contains unsupported characters.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: Codes.ValueInvalid,
            message: "Phone number format is invalid.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ValueRequired = "PhoneNumber.Value.Required";
        public const string ValueTooLong = "PhoneNumber.Value.TooLong";
        public const string ValueInvalid = "PhoneNumber.Value.Invalid";
    }
}