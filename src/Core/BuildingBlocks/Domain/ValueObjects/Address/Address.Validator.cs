using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Domain.ValueObjects.Address;

/// <summary>Validates address fields without modifying the supplied value.</summary>
public static class AddressValidator
{
    #region Public Methods

    /// <summary>Returns success when every address field satisfies its constraints.</summary>
    /// <param name="address">The address to validate.</param>
    /// <returns>The unchanged address on success, or its validation errors.</returns>
    public static Result<Address> Validate(Address address)
    {
        return Result<Address>.Success(address)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(address.Line1),
                error: AddressOutcome.Failure.Line1Required)

            .Ensure(
                predicate: _ =>
                    address.Line1!.Length <=
                    AddressConstant.Constraints.Line1.MaxLength,
                error: AddressOutcome.Failure.Line1TooLong)

            .Ensure(
                predicate: _ =>
                    address.Line2 is null ||
                    address.Line2.Length <=
                    AddressConstant.Constraints.Line2.MaxLength,
                error: AddressOutcome.Failure.Line2TooLong)

            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(address.City),
                error: AddressOutcome.Failure.CityRequired)

            .Ensure(
                predicate: _ =>
                    address.City!.Length <=
                    AddressConstant.Constraints.City.MaxLength,
                error: AddressOutcome.Failure.CityTooLong)

            .Ensure(
                predicate: _ =>
                    address.Region is null ||
                    address.Region.Length <=
                    AddressConstant.Constraints.Region.MaxLength,
                error: AddressOutcome.Failure.RegionTooLong)

            .Ensure(
                predicate: _ =>
                    !string.IsNullOrWhiteSpace(address.PostalCode.Value) &&
                    Regex.IsMatch(
                        address.PostalCode.Value!,
                        PostalCodeConstant.Patterns.Code),
                error: AddressOutcome.Failure.PostalCodeInvalid)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        address.CountryCode ?? string.Empty,
                        AddressConstant.Patterns.CountryCode),
                error: AddressOutcome.Failure.CountryCodeInvalid);
    }

    #endregion
}