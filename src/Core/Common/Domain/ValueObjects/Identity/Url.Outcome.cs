namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>Result constants for the <see cref="Url"/> value object.</summary>
public static class UrlOutcome
{
    /// <summary>Failure error constants for the <see cref="Url"/> value object.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the URL is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: "Url.Value.Required",
            message: "Url must be specified.");

        /// <summary>Error returned when the URL exceeds its maximum length.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: "Url.Value.TooLong",
            message:
                $"Url cannot exceed {UrlConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error returned when the URL is not an absolute HTTP or HTTPS URL.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: "Url.Value.Invalid",
            message: "Url must be an absolute http(s) address.");

        #endregion
    }
}