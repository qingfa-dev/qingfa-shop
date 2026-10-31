namespace Core.BuildingBlocks.Domain.ValueObjects.Localization;

/// <summary>Groups validation errors for <see cref="Locale"/> creation.</summary>
public static class LocaleOutcome
{
    /// <summary>Contains locale-code validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the locale code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: Codes.CodeRequired,
            message: "Locale must be specified.");

        /// <summary>Error returned when the locale code exceeds thirty-five characters.</summary>
        public static Error CodeTooLong => Error.BadRequest(
            code: Codes.CodeTooLong,
            message:
                $"Locale cannot exceed {LocaleConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the code does not match the configured locale syntax.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: Codes.CodeInvalid,
            message:
                "Locale must be a BCP-47 tag such as 'en', 'en-US' or 'zh-Hans-CN'.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string CodeRequired = "Locale.Code.Required";
        public const string CodeTooLong = "Locale.Code.TooLong";
        public const string CodeInvalid = "Locale.Code.Invalid";
    }
}