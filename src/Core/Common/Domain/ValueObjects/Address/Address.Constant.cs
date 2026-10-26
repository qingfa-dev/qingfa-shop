namespace Core.Common.Domain.ValueObjects.Address;

/// <summary>Defines address field limits and accepted country-code syntax.</summary>
public static class AddressConstant
{
    /// <summary>Maximum accepted address field lengths.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for the primary address line.</summary>
        public static class Line1
        {
            /// <summary>Maximum length of the primary address line.</summary>
            public const int MaxLength = 256;
        }

        /// <summary>Length constraints for the secondary address line.</summary>
        public static class Line2
        {
            /// <summary>Maximum length of the secondary address line.</summary>
            public const int MaxLength = 256;
        }

        /// <summary>Length constraints for the city.</summary>
        public static class City
        {
            /// <summary>Maximum length of a city name.</summary>
            public const int MaxLength = 128;
        }

        /// <summary>Length constraints for the region.</summary>
        public static class Region
        {
            /// <summary>Maximum length of a region name.</summary>
            public const int MaxLength = 128;
        }

        /// <summary>Length constraints for the country code.</summary>
        public static class CountryCode
        {
            /// <summary>Maximum length of a country code.</summary>
            public const int MaxLength = 2;
        }
    }

    /// <summary>Default values for address creation.</summary>
    public static class Defaults
    {
        /// <summary>Notes that country codes are normalized to uppercase; no default value is applied.</summary>
        public const string CountryCodeNormalization = "Uppercase";
    }

    /// <summary>Regular expressions used to validate address codes.</summary>
    public static class Patterns
    {
        /// <summary>Accepts exactly two uppercase ASCII letters.</summary>
        public const string CountryCode = @"^[A-Z]{2}$";
    }
}