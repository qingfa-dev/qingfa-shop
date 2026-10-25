namespace Core.Common.Domain.ValueObjects.Address;

/// <summary>A postal address with a required first line, city, postal code, and country code.</summary>
/// <param name="Line1">The primary street address line; surrounding whitespace is removed by <see cref="Create"/>.</param>
/// <param name="Line2">An optional secondary address line.</param>
/// <param name="City">The city or locality.</param>
/// <param name="Region">The optional region or subdivision.</param>
/// <param name="PostalCode">The postal code value.</param>
/// <param name="CountryCode">The two-letter country code, normalized to uppercase by <see cref="Create"/>.</param>
public readonly partial record struct Address(
    string? Line1,
    string? Line2,
    string? City,
    string? Region,
    PostalCode PostalCode,
    string? CountryCode)
{
    /// <summary>Creates an address after validating its required fields and country/postal codes.</summary>
    /// <param name="line1">The required primary street address line.</param>
    /// <param name="line2">The optional secondary address line.</param>
    /// <param name="city">The required city or locality.</param>
    /// <param name="region">The optional region or subdivision.</param>
    /// <param name="postalCode">The postal code to validate.</param>
    /// <param name="countryCode">The two-letter country code; trimmed and uppercased.</param>
    /// <returns>The normalized address, or the validation errors.</returns>
    public static Result<Address> Create(
        string? line1,
        string? line2,
        string? city,
        string? region,
        PostalCode postalCode,
        string? countryCode)
    {
        var candidate = new Address(
            line1?.Trim(),
            line2?.Trim(),
            city?.Trim(),
            region?.Trim(),
            postalCode,
            countryCode?.Trim().ToUpperInvariant());

        return AddressValidator.Validate(candidate);
    }
}