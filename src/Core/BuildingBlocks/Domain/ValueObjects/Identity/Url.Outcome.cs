namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>Result constants for the <see cref="Url"/> value object.</summary>
public static class UrlOutcome
{
    /// <summary>Failure error constants for the <see cref="Url"/> value object.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the URL is missing.</summary>
        public static Error ValueRequired => Error.BadRequest(
            code: Codes.ValueRequired,
            message: "Url must be specified.");

        /// <summary>Error returned when the URL exceeds its maximum length.</summary>
        public static Error ValueTooLong => Error.BadRequest(
            code: Codes.ValueTooLong,
            message:
                $"Url cannot exceed {UrlConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error returned when the URL is not an absolute HTTP or HTTPS URL.</summary>
        public static Error ValueInvalid => Error.BadRequest(
            code: Codes.ValueInvalid,
            message: "Url must be an absolute http(s) address.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string ValueRequired = "Url.Value.Required";
        public const string ValueTooLong = "Url.Value.TooLong";
        public const string ValueInvalid = "Url.Value.Invalid";
    }
}