namespace Core.Common.Concerns.Content.Localizable;

/// <summary>Result constants for the <see cref="Localizable"/> concern.</summary>
public static class LocalizableOutcome
{
    /// <summary>Failure error constants for the <see cref="Localizable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the localizable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Localizable.Entity.Required",
            message: "Localizable entity is required.");

        /// <summary>Error when the localized property is not specified.</summary>
        public static Error PropertyRequired => Error.UnprocessableEntity(
            code: "Localizable.Property.Required",
            message: "Localized property must be specified.");

        /// <summary>Error when the localized property is shorter than the minimum length.</summary>
        public static Error PropertyTooShort => Error.UnprocessableEntity(
            code: "Localizable.Property.TooShort",
            message:
                $"Localized property must be at least {LocalizableConstant.Constraints.Property.MinLength} character(s).");

        /// <summary>Error when the localized property exceeds the maximum length.</summary>
        public static Error PropertyTooLong => Error.UnprocessableEntity(
            code: "Localizable.Property.TooLong",
            message:
                $"Localized property cannot exceed {LocalizableConstant.Constraints.Property.MaxLength} characters.");

        /// <summary>Error when the locale is not specified.</summary>
        public static Error LocaleRequired => Error.UnprocessableEntity(
            code: "Localizable.Locale.Required",
            message: "Locale must be specified.");

        /// <summary>Error when the locale is shorter than the minimum length.</summary>
        public static Error LocaleTooShort => Error.UnprocessableEntity(
            code: "Localizable.Locale.TooShort",
            message:
                $"Locale must be at least {LocalizableConstant.Constraints.Locale.MinLength} characters.");

        /// <summary>Error when the locale exceeds the maximum length.</summary>
        public static Error LocaleTooLong => Error.UnprocessableEntity(
            code: "Localizable.Locale.TooLong",
            message:
                $"Locale cannot exceed {LocalizableConstant.Constraints.Locale.MaxLength} characters.");

        /// <summary>Error when the locale is not a valid BCP 47 identifier.</summary>
        public static Error LocaleInvalid => Error.UnprocessableEntity(
            code: "Localizable.Locale.Invalid",
            message: "Locale must be a valid BCP 47 identifier (e.g. \"en\", \"en-US\").");

        /// <summary>Error when the localized value exceeds the maximum length.</summary>
        public static Error ValueTooLong => Error.UnprocessableEntity(
            code: "Localizable.Value.TooLong",
            message:
                $"Localized value cannot exceed {LocalizableConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error when the localized value is not found.</summary>
        public static Error ValueNotFound => Error.UnprocessableEntity(
            code: "Localizable.Value.NotFound",
            message: "Localized value is not present.");

        /// <summary>Error when the entity already has the maximum number of translations.</summary>
        public static Error TranslationsTooMany => Error.UnprocessableEntity(
            code: "Localizable.Translations.TooMany",
            message:
                $"Cannot store more than {LocalizableConstant.Constraints.Collection.MaxTranslations} translations.");

        #endregion
    }
}