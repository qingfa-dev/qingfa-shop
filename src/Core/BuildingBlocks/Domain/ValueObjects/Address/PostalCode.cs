namespace Core.BuildingBlocks.Domain.ValueObjects.Address;

/// <summary>A validated postal or ZIP code.</summary>
/// <param name="Value">The postal code; <see cref="Create"/> trims and uppercases it.</param>
public readonly partial record struct PostalCode(string? Value)
{
    /// <summary>Creates a normalized postal code with at most ten allowed characters.</summary>
    /// <param name="value">The candidate postal code.</param>
    /// <returns>The normalized code, or a validation error.</returns>
    public static Result<PostalCode> Create(string? value)
    {
        var candidate = new PostalCode(value?.Trim().ToUpperInvariant());

        return PostalCodeValidator.Validate(candidate);
    }
}