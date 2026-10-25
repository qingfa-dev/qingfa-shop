namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>Result constants for the <see cref="EmailAddress"/> value object.</summary>
public static class EmailAddressOutcome
{
    /// <summary>Failure error constants for the <see cref="EmailAddress"/> value object.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the address is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: "EmailAddress.Value.Required",
            message: "Email address must be specified.");

        /// <summary>Error returned when the address exceeds its maximum length.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: "EmailAddress.Value.TooLong",
            message:
                $"Email address cannot exceed {EmailAddressConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error returned when the address does not match the configured syntax.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: "EmailAddress.Value.Invalid",
            message: "Email address format is invalid.");

        #endregion
    }
}