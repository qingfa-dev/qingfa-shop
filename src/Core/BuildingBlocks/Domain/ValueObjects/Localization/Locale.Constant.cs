namespace Core.BuildingBlocks.Domain.ValueObjects.Localization;

/// <summary>Defines locale-code length, default and syntax constraints.</summary>
public static class LocaleConstant
{
    /// <summary>Constraint values for locale codes.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a locale code.</summary>
        public static class Code
        {
            /// <summary>Minimum length for a locale code.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum accepted locale-code length.</summary>
            public const int MaxLength = 35;
        }
    }

    /// <summary>Default values for locale codes.</summary>
    public static class Defaults
    {
        /// <summary>Fallback locale used when no locale is specified by a caller.</summary>
        public const string Default = "en";
    }

    /// <summary>Regular expressions used to validate locale codes.</summary>
    public static class Patterns
    {
        /// <summary>Accepts the configured language and optional subtags syntax.</summary>
        public const string Code =
            @"^[a-z]{2,3}(?:-[A-Za-z]{4})?(?:-(?:[A-Z]{2}|[0-9]{3}))?$";
    }
}