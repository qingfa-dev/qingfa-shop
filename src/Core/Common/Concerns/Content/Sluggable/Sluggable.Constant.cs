namespace Core.Common.Concerns.Content.Sluggable;

/// <summary>Constants for the <see cref="Sluggable"/> concern.</summary>
public static class SluggableConstant
{
    /// <summary>Constraint values for the <see cref="Sluggable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a slug.</summary>
        public static class Slug
        {
            /// <summary>Minimum length for a slug.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for a slug.</summary>
            public const int MaxLength = 200;
        }
    }

    /// <summary>Default values for the <see cref="Sluggable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Suggested fallback slug when a caller needs one for empty input.</summary>
        public const string FallbackSlug = "n-a";
    }

    /// <summary>Regex patterns for the <see cref="Sluggable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for slug validation (lowercase alphanumerics, single hyphens).</summary>
        public const string Slug = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
    }
}