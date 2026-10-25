namespace Core.Common.Domain.ValueObjects.Address;

/// <summary>Groups validation errors for <see cref="Address"/> creation.</summary>
public static class AddressOutcome
{
    /// <summary>Contains address validation errors.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error returned when the primary address line is missing.</summary>
        public static Error Line1Required => Error.BadRequest(
            code: "Address.Line1.Required",
            message: "Address line 1 must be specified.");

        /// <summary>Error returned when the primary address line exceeds its limit.</summary>
        public static Error Line1TooLong => Error.BadRequest(
            code: "Address.Line1.TooLong",
            message:
                $"Address line 1 cannot exceed {AddressConstant.Constraints.Line1.MaxLength} characters.");

        /// <summary>Error returned when the secondary address line exceeds its limit.</summary>
        public static Error Line2TooLong => Error.BadRequest(
            code: "Address.Line2.TooLong",
            message:
                $"Address line 2 cannot exceed {AddressConstant.Constraints.Line2.MaxLength} characters.");

        /// <summary>Error returned when the city is missing.</summary>
        public static Error CityRequired => Error.BadRequest(
            code: "Address.City.Required",
            message: "City must be specified.");

        /// <summary>Error returned when the city exceeds its limit.</summary>
        public static Error CityTooLong => Error.BadRequest(
            code: "Address.City.TooLong",
            message:
                $"City cannot exceed {AddressConstant.Constraints.City.MaxLength} characters.");

        /// <summary>Error returned when the region exceeds its limit.</summary>
        public static Error RegionTooLong => Error.BadRequest(
            code: "Address.Region.TooLong",
            message:
                $"Region cannot exceed {AddressConstant.Constraints.Region.MaxLength} characters.");

        /// <summary>Error returned when the postal code is invalid.</summary>
        public static Error PostalCodeInvalid => Error.BadRequest(
            code: "Address.PostalCode.Invalid",
            message: "Postal code is invalid.");

        /// <summary>Error returned when the country code is not two uppercase letters.</summary>
        public static Error CountryCodeInvalid => Error.BadRequest(
            code: "Address.CountryCode.Invalid",
            message: "Country code must be a 2-letter ISO-3166 code.");

        #endregion
    }
}