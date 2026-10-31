namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>Constants for the <see cref="EmailAddress"/> value object.</summary>
public static class EmailAddressConstant
{
    /// <summary>Constraint values for the <see cref="EmailAddress"/> value object.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for an email address value.</summary>
        public static class Value
        {
            /// <summary>Minimum length for an email address value.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for an email address value.</summary>
            public const int MaxLength = 254;
        }
    }

    /// <summary>Default values for the <see cref="EmailAddress"/> value object.</summary>
    public static class Defaults
    {
        // Note: Create normalizes input by trimming surrounding whitespace and lowercasing it; no fallback value is applied.
    }

    /// <summary>Regex patterns for the <see cref="EmailAddress"/> value object.</summary>
    public static class Patterns
    {
        /// <summary>Accepts a non-whitespace local and domain part separated by an at sign and a dotted alphabetic suffix.</summary>
        public const string Address = @"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$";
    }
}