namespace Core.Common.Domain.ValueObjects.Localization;

/// <summary>Groups validation errors for <see cref="Language"/> creation.</summary>
public static class LanguageOutcome
{
    /// <summary>Contains language-code validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the language code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: "Language.Code.Required",
            message: "Language must be specified.");

        /// <summary>Error returned when the language code exceeds the configured maximum length.</summary>
        public static Error CodeTooLong => Error.BadRequest(
            code: "Language.Code.TooLong",
            message:
                $"Language cannot exceed {LanguageConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the code is not two or three lowercase letters.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: "Language.Code.Invalid",
            message:
                "Language must be a lowercase ISO-639 subtag such as 'en' or 'fra'.");

        #endregion
    }
}