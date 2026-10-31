namespace Core.BuildingBlocks.Domain.ValueObjects.Identity;

/// <summary>An email address normalized by trimming and lowercasing its value.</summary>
/// <param name="Value">The normalized address created by <see cref="Create"/>.</param>
public readonly partial record struct EmailAddress(string? Value)
{
    /// <summary>Creates an email address using the Core's configured syntax and length checks.</summary>
    /// <param name="value">The candidate address; surrounding whitespace is removed and text is lowercased.</param>
    /// <returns>The normalized address, or a validation error.</returns>
    public static Result<EmailAddress> Create(string? value)
    {
        var candidate = new EmailAddress(value?.Trim().ToLowerInvariant());

        return EmailAddressValidator.Validate(candidate);
    }
}