namespace Core.BuildingBlocks.Domain.ValueObjects.Address;

/// <summary>Defines postal-code length and character constraints.</summary>
public static class PostalCodeConstant
{
    /// <summary>Postal-code length limits.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for the postal code value.</summary>
        public static class Code
        {
            /// <summary>Minimum accepted number of characters.</summary>
            public const int MinLength = 1;

            /// <summary>Maximum accepted number of characters.</summary>
            public const int MaxLength = 10;
        }
    }

    /// <summary>Default values for postal-code creation.</summary>
    public static class Defaults
    {
        /// <summary>Notes that postal codes are normalized to uppercase; no default value is applied.</summary>
        public const string ValueNormalization = "Uppercase";
    }

    /// <summary>Regular expressions used to validate postal codes.</summary>
    public static class Patterns
    {
        /// <summary>Accepts an alphanumeric first character followed by up to nine alphanumeric, space, or hyphen characters.</summary>
        public const string Code = @"^[A-Za-z0-9][A-Za-z0-9 -]{0,9}$";
    }
}