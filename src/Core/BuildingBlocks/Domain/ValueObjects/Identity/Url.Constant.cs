namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>Constants for the <see cref="Url"/> value object.</summary>
public static class UrlConstant
{
    /// <summary>Constraint values for the <see cref="Url"/> value object.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a URL value.</summary>
        public static class Value
        {
            /// <summary>Minimum length for a URL value.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for a URL value.</summary>
            public const int MaxLength = 2048;
        }
    }

    /// <summary>Default values for the <see cref="Url"/> value object.</summary>
    public static class Defaults
    {
        // Note: Create normalizes input by trimming surrounding whitespace; no fallback value is applied.
    }
}