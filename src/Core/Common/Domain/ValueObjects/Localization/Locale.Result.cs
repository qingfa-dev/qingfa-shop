namespace Core.Common.Domain.ValueObjects.Localization;

/// <summary>Groups validation errors for <see cref="Locale"/> creation.</summary>
public static class LocaleResult
{
    /// <summary>Contains locale-code validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the locale code is missing.</summary>
        public static Error CodeRequired => Error.BadRequest(
            code: "Locale.Code.Required",
            message: "Locale must be specified.");

        /// <summary>Error returned when the locale code exceeds thirty-five characters.</summary>
        public static Error CodeTooLong => Error.BadRequest(
            code: "Locale.Code.TooLong",
            message:
                $"Locale cannot exceed {LocaleConstant.Constraints.Code.MaxLength} characters.");

        /// <summary>Error returned when the code does not match the configured locale syntax.</summary>
        public static Error CodeInvalid => Error.BadRequest(
            code: "Locale.Code.Invalid",
            message:
                "Locale must be a BCP-47 tag such as 'en', 'en-US' or 'zh-Hans-CN'.");

        #endregion
    }
}