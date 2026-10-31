namespace Core.BuildingBlocks.Domain.ValueObjects.Localization;

/// <summary>Groups validation errors for <see cref="Language"/> creation.</summary>
public static class LanguageOutcome
{
    /// <summary>Contains language-code validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the language code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: Codes.CodeRequired,
            message: "Language must be specified.");

        /// <summary>Error returned when the language code exceeds the configured maximum length.</summary>
        public static Error CodeTooLong => Error.BadRequest(
            code: Codes.CodeTooLong,
            message:
                $"Language cannot exceed {LanguageConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the code is not two or three lowercase letters.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: Codes.CodeInvalid,
            message:
                "Language must be a lowercase ISO-639 subtag such as 'en' or 'fra'.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string CodeRequired = "Language.Code.Required";
        public const string CodeTooLong = "Language.Code.TooLong";
        public const string CodeInvalid = "Language.Code.Invalid";
    }
}