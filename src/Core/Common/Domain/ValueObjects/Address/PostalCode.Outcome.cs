namespace Core.Common.Domain.ValueObjects.Address;

/// <summary>Groups validation errors for <see cref="PostalCode"/> creation.</summary>
public static class PostalCodeOutcome
{
    /// <summary>Contains postal-code validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the postal code is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: "PostalCode.Value.Required",
            message: "Postal code must be specified.");

        /// <summary>Error returned when the postal code exceeds ten characters.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: "PostalCode.Value.TooLong",
            message:
                $"Postal code cannot exceed {PostalCodeConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the postal code contains unsupported characters.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: "PostalCode.Value.Invalid",
            message:
                "Postal code may contain only letters, digits, spaces and hyphens.");

        #endregion
    }
}