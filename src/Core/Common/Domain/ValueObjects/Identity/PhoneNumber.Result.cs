namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>Result constants for the <see cref="PhoneNumber"/> value object.</summary>
public static class PhoneNumberResult
{
    /// <summary>Failure error constants for the <see cref="PhoneNumber"/> value object.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the phone number is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: "PhoneNumber.Value.Required",
            message: "Phone number must be specified.");

        /// <summary>Error returned when the phone number exceeds its maximum length.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: "PhoneNumber.Value.TooLong",
            message:
                $"Phone number cannot exceed {PhoneNumberConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error returned when the phone number contains unsupported characters.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: "PhoneNumber.Value.Invalid",
            message: "Phone number format is invalid.");

        #endregion
    }
}