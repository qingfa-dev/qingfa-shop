using System.Text.RegularExpressions;

namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>Validates email addresses without modifying them.</summary>
public static class EmailAddressValidator
{
    /// <summary>Returns success when the address is present, within the length limit, and matches the configured pattern.</summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>The unchanged address on success, or its validation errors.</returns>
    public static Result<EmailAddress> Validate(EmailAddress email)
    {
        return Result<EmailAddress>.Success(email)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(email.Value),
                error: EmailAddressOutcome.Failure.ValueRequired)

            .Ensure(
                predicate: _ =>
                    email.Value!.Length <=
                    EmailAddressConstant.Constraints.Value.MaxLength,
                error: EmailAddressOutcome.Failure.ValueTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(
                        email.Value!,
                        EmailAddressConstant.Patterns.Address),
                error: EmailAddressOutcome.Failure.ValueInvalid);
    }
}