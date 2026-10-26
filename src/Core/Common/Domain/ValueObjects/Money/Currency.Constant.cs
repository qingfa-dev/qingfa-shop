namespace Core.Common.Domain.ValueObjects.Money;

/// <summary>Defines syntax constraints for currency codes.</summary>
public static class CurrencyConstant
{
    /// <summary>Currency-code length limits.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a currency code.</summary>
        public static class Code
        {
            /// <summary>Minimum length for a currency code.</summary>
            public const int MinLength = 3;
            /// <summary>Maximum length for a currency code.</summary>
            public const int MaxLength = 3;
        }
    }

    /// <summary>Default values for currency codes.</summary>
    public static class Defaults
    {
        /// <summary>Fallback currency code when a caller needs one for empty input.</summary>
        public const string DefaultCode = "USD";
    }

    /// <summary>Regular expressions used to validate currency codes.</summary>
    public static class Patterns
    {
        /// <summary>Accepts exactly three uppercase ASCII letters.</summary>
        public const string Code = @"^[A-Z]{3}$";
    }
}