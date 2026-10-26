namespace Core.Common.Domain.ValueObjects.Identity;

/// <summary>A phone number retained after surrounding whitespace is removed.</summary>
/// <param name="Value">The trimmed phone-number text created by <see cref="Create"/>.</param>
public readonly partial record struct PhoneNumber(string? Value)
{
    /// <summary>Creates a phone number within the configured length and character constraints.</summary>
    /// <param name="value">The candidate phone number; surrounding whitespace is removed.</param>
    /// <returns>The normalized number, or a validation error.</returns>
    public static Result<PhoneNumber> Create(string? value)
    {
        var candidate = new PhoneNumber(value?.Trim());

        return PhoneNumberValidator.Validate(candidate);
    }
}