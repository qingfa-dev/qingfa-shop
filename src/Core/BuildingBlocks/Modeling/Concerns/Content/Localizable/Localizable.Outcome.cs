namespace Core.BuildingBlocks.Modeling.Concerns.Content.Localizable;

/// <summary>Result constants for the <see cref="Localizable"/> concern.</summary>
public static class LocalizableOutcome
{
    /// <summary>Failure error constants for the <see cref="Localizable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the localizable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Localizable entity is required.");

        /// <summary>Error when the localized property is not specified.</summary>
        public static Error PropertyRequired => Error.UnprocessableEntity(
            code: Codes.PropertyRequired,
            message: "Localized property must be specified.");

        /// <summary>Error when the localized property is shorter than the minimum length.</summary>
        public static Error PropertyTooShort => Error.UnprocessableEntity(
            code: Codes.PropertyTooShort,
            message:
                $"Localized property must be at least {LocalizableConstant.Constraints.Property.MinLength} character(s).");

        /// <summary>Error when the localized property exceeds the maximum length.</summary>
        public static Error PropertyTooLong => Error.UnprocessableEntity(
            code: Codes.PropertyTooLong,
            message:
                $"Localized property cannot exceed {LocalizableConstant.Constraints.Property.MaxLength} characters.");

        /// <summary>Error when the locale is not specified.</summary>
        public static Error LocaleRequired => Error.UnprocessableEntity(
            code: Codes.LocaleRequired,
            message: "Locale must be specified.");

        /// <summary>Error when the locale is shorter than the minimum length.</summary>
        public static Error LocaleTooShort => Error.UnprocessableEntity(
            code: Codes.LocaleTooShort,
            message:
                $"Locale must be at least {LocalizableConstant.Constraints.Locale.MinLength} characters.");

        /// <summary>Error when the locale exceeds the maximum length.</summary>
        public static Error LocaleTooLong => Error.UnprocessableEntity(
            code: Codes.LocaleTooLong,
            message:
                $"Locale cannot exceed {LocalizableConstant.Constraints.Locale.MaxLength} characters.");

        /// <summary>Error when the locale is not a valid BCP 47 identifier.</summary>
        public static Error LocaleInvalid => Error.UnprocessableEntity(
            code: Codes.LocaleInvalid,
            message: "Locale must be a valid BCP 47 identifier (e.g. \"en\", \"en-US\").");

        /// <summary>Error when the localized value exceeds the maximum length.</summary>
        public static Error ValueTooLong => Error.UnprocessableEntity(
            code: Codes.ValueTooLong,
            message:
                $"Localized value cannot exceed {LocalizableConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error when the localized value is not found.</summary>
        public static Error ValueNotFound => Error.UnprocessableEntity(
            code: Codes.ValueNotFound,
            message: "Localized value is not present.");

        /// <summary>Error when the entity already has the maximum number of translations.</summary>
        public static Error TranslationsTooMany => Error.UnprocessableEntity(
            code: Codes.TranslationsTooMany,
            message:
                $"Cannot store more than {LocalizableConstant.Constraints.Collection.MaxTranslations} translations.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Localizable.Entity.Required";
        public const string PropertyRequired = "Localizable.Property.Required";
        public const string PropertyTooShort = "Localizable.Property.TooShort";
        public const string PropertyTooLong = "Localizable.Property.TooLong";
        public const string LocaleRequired = "Localizable.Locale.Required";
        public const string LocaleTooShort = "Localizable.Locale.TooShort";
        public const string LocaleTooLong = "Localizable.Locale.TooLong";
        public const string LocaleInvalid = "Localizable.Locale.Invalid";
        public const string ValueTooLong = "Localizable.Value.TooLong";
        public const string ValueNotFound = "Localizable.Value.NotFound";
        public const string TranslationsTooMany = "Localizable.Translations.TooMany";
    }
}