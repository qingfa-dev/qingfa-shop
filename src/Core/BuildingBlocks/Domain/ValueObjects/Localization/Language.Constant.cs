namespace Core.BuildingBlocks.Domain.ValueObjects.Localization;

/// <summary>Defines language-code length, default and syntax constraints.</summary>
public static class LanguageConstant
{
    /// <summary>Constraint values for language codes.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a language code.</summary>
        public static class Code
        {
            /// <summary>Minimum length for a language code.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum accepted language-code length.</summary>
            public const int MaxLength = 8;
        }
    }

    /// <summary>Default values for language codes.</summary>
    public static class Defaults
    {
        /// <summary>Fallback language used when no language is specified by a caller.</summary>
        public const string Default = "en";
    }

    /// <summary>Regular expressions used to validate language codes.</summary>
    public static class Patterns
    {
        /// <summary>Accepts two or three lowercase ASCII letters.</summary>
        public const string Code = @"^[a-z]{2,3}$";
    }
}