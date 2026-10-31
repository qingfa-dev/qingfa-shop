namespace Core.BuildingBlocks.Domain.ValueObjects.Address;

/// <summary>Groups validation errors for <see cref="Address"/> creation.</summary>
public static class AddressOutcome
{
    /// <summary>Contains address validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the primary address line is missing.</summary>
        public static Error Line1Required => Error.BadRequest(
            code: Codes.Line1Required,
            message: "Address line 1 must be specified.");

        /// <summary>Error returned when the primary address line exceeds its limit.</summary>
        public static Error Line1TooLong => Error.BadRequest(
            code: Codes.Line1TooLong,
            message:
                $"Address line 1 cannot exceed {AddressConstant.Constraints.Line1.MaxLength} characters.");

        /// <summary>Error returned when the secondary address line exceeds its limit.</summary>
        public static Error Line2TooLong => Error.BadRequest(
            code: Codes.Line2TooLong,
            message:
                $"Address line 2 cannot exceed {AddressConstant.Constraints.Line2.MaxLength} characters.");

        /// <summary>Error returned when the city is missing.</summary>
        public static Error CityRequired => Error.BadRequest(
            code: Codes.CityRequired,
            message: "City must be specified.");

        /// <summary>Error returned when the city exceeds its limit.</summary>
        public static Error CityTooLong => Error.BadRequest(
            code: Codes.CityTooLong,
            message:
                $"City cannot exceed {AddressConstant.Constraints.City.MaxLength} characters.");

        /// <summary>Error returned when the region exceeds its limit.</summary>
        public static Error RegionTooLong => Error.BadRequest(
            code: Codes.RegionTooLong,
            message:
                $"Region cannot exceed {AddressConstant.Constraints.Region.MaxLength} characters.");

        /// <summary>Error returned when the postal code is invalid.</summary>
        public static Error PostalCodeInvalid => Error.BadRequest(
            code: Codes.PostalCodeInvalid,
            message: "Postal code is invalid.");

        /// <summary>Error returned when the country code is not two uppercase letters.</summary>
        public static Error CountryCodeInvalid => Error.BadRequest(
            code: Codes.CountryCodeInvalid,
            message: "Country code must be a 2-letter ISO-3166 code.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string Line1Required = "Address.Line1.Required";
        public const string Line1TooLong = "Address.Line1.TooLong";
        public const string Line2TooLong = "Address.Line2.TooLong";
        public const string CityRequired = "Address.City.Required";
        public const string CityTooLong = "Address.City.TooLong";
        public const string RegionTooLong = "Address.Region.TooLong";
        public const string PostalCodeInvalid = "Address.PostalCode.Invalid";
        public const string CountryCodeInvalid = "Address.CountryCode.Invalid";
    }
}