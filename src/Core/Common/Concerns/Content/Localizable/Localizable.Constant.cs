namespace Core.Common.Concerns.Content.Localizable;

/// <summary>Constants for the <see cref="Localizable"/> concern.</summary>
public static class LocalizableConstant
{
    /// <summary>Constraint values for the <see cref="Localizable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a localized property name.</summary>
        public static class Property
        {
            /// <summary>Minimum length for a localized property name.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for a localized property name.</summary>
            public const int MaxLength = 128;
        }

        /// <summary>Length constraints for a locale identifier.</summary>
        public static class Locale
        {
            /// <summary>Minimum length for a locale identifier.</summary>
            public const int MinLength = 2;
            /// <summary>Maximum length for a locale identifier.</summary>
            public const int MaxLength = 35;
        }

        /// <summary>Length constraints for a localized value.</summary>
        public static class Value
        {
            /// <summary>Maximum length for a localized value.</summary>
            public const int MaxLength = 4000;
        }

        /// <summary>Count constraints for the localized-value collection.</summary>
        public static class Collection
        {
            /// <summary>Maximum number of (property, locale) translations per entity.</summary>
            public const int MaxTranslations = 200;
        }
    }

    /// <summary>Default values for the <see cref="Localizable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Fallback locale used when no locale is specified by a caller.</summary>
        public const string Locale = "en";
    }

    /// <summary>Regex patterns for the <see cref="Localizable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for BCP 47 locale validation (e.g. "en", "en-US").</summary>
        public const string Locale = @"^[A-Za-z]{2,8}(?:-[A-Za-z0-9]{2,8})*$";
    }
}