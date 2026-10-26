namespace Core.Common.Domain.ValueObjects.Money;

/// <summary>Defines currency constraints for money values.</summary>
public static class MoneyConstant
{
    /// <summary>Money validation limits.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for the money currency code.</summary>
        public static class Currency
        {
            /// <summary>Minimum length for the money currency code.</summary>
            public const int MinLength = 3;
            /// <summary>Maximum length for the money currency code.</summary>
            public const int MaxLength = 3;
        }
    }

    /// <summary>Default values for money values.</summary>
    public static class Defaults
    {
        /// <summary>Fallback currency code when a caller needs one for empty input.</summary>
        public const string DefaultCurrencyCode = "USD";
    }

    /// <summary>Regular expressions used to validate money values.</summary>
    public static class Patterns
    {
        /// <summary>Accepts exactly three uppercase ASCII letters.</summary>
        public const string CurrencyCode = CurrencyConstant.Patterns.Code;
    }
}