namespace Core.Common.Concerns.Foundation.Referenceable;

/// <summary>Constants for the Referenceable concern.</summary>
public static class ReferenceableConstant
{
    /// <summary>Constraint values for reference validation.</summary>
    public static class Constraints
    {
        /// <summary>Maximum allowed length for a reference string.</summary>
        public const int MaxReferenceLength = Reference.MaxLength;

        /// <summary>Reference length constraints.</summary>
        public static class Reference
        {
            /// <summary>Minimum allowed length for a reference string.</summary>
            public const int MinLength = 1;

            /// <summary>Maximum allowed length for a reference string.</summary>
            public const int MaxLength = 128;
        }
    }

    /// <summary>Default values for the Referenceable concern.</summary>
    public static class Defaults
    {
        /// <summary>Empty reference value used when clearing.</summary>
        public const string Empty = "";
    }

    /// <summary>Regex patterns for the Referenceable concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern rejecting leading or trailing whitespace.</summary>
        public const string Trimmed = @"^\S(?:.*\S)?$";

        /// <summary>Pattern for the allowed charset: letters, digits, space, '-', '_', '/', '#', '.', ':'.</summary>
        public const string AllowedCharset = @"^[A-Za-z0-9\-_/#:. ]+$";
    }
}