namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>Constants for the <see cref="PhoneNumber"/> value object.</summary>
public static class PhoneNumberConstant
{
    /// <summary>Constraint values for the <see cref="PhoneNumber"/> value object.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a phone number value.</summary>
        public static class Value
        {
            // Note: No MinLength is declared; Patterns.Number already encodes the minimum of 5 characters.
            /// <summary>Maximum length for a phone number value.</summary>
            public const int MaxLength = 20;
        }
    }

    /// <summary>Default values for the <see cref="PhoneNumber"/> value object.</summary>
    public static class Defaults
    {
        // Note: Create normalizes input by trimming surrounding whitespace; no fallback value is applied.
    }

    /// <summary>Regex patterns for the <see cref="PhoneNumber"/> value object.</summary>
    public static class Patterns
    {
        /// <summary>Accepts an optional plus sign followed by digits, spaces, parentheses, or hyphens.</summary>
        public const string Number = @"^\+?[0-9 ()\-]{5,20}$";
    }
}