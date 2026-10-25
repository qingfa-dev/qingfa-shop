using System.Text.RegularExpressions;

namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>Validates phone numbers without modifying them.</summary>
public static class PhoneNumberValidator
{
    /// <summary>Returns success when a number is present and matches the configured constraints.</summary>
    /// <param name="phone">The phone number to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<PhoneNumber> Validate(PhoneNumber phone)
    {
        return Result<PhoneNumber>.Success(phone)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(phone.Value),
                error: PhoneNumberResult.Failure.ValueRequired)

            .Ensure(
                predicate: _ =>
                    phone.Value!.Length <=
                    PhoneNumberConstant.Constraints.Value.MaxLength,
                error: PhoneNumberResult.Failure.ValueTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        phone.Value!,
                        PhoneNumberConstant.Patterns.Number),
                error: PhoneNumberResult.Failure.ValueInvalid);
    }
}