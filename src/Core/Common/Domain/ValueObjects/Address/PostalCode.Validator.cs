using System.Text.RegularExpressions;

namespace Core.Common.Domain.ValueObjects.Address;

/// <summary>Validates postal-code values without modifying them.</summary>
public static class PostalCodeValidator
{
    #region Public Methods

    /// <summary>Returns success when a postal code is present and matches the configured format.</summary>
    /// <param name="postalCode">The postal code to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<PostalCode> Validate(PostalCode postalCode)
    {
        return Result<PostalCode>.Success(postalCode)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(postalCode.Value),
                error: PostalCodeOutcome.Failure.ValueRequired)

            .Ensure(
                predicate: _ =>
                    postalCode.Value!.Length <=
                    PostalCodeConstant.Constraints.Code.MaxLength,
                error: PostalCodeOutcome.Failure.ValueTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        postalCode.Value!,
                        PostalCodeConstant.Patterns.Code),
                error: PostalCodeOutcome.Failure.ValueInvalid);
    }

    #endregion
}