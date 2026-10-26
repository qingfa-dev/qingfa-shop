namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>A normalized, validated unit code.</summary>
/// <param name="Code">The lowercase code created by <see cref="Create"/>.</param>
public readonly partial record struct Unit(string? Code)
{
    /// <summary>Creates a unit code after trimming, lowercasing, and validating it.</summary>
    /// <param name="code">The candidate unit code.</param>
    /// <returns>The normalized unit, or a validation error.</returns>
    public static Result<Unit> Create(string? code)
    {
        var candidate = new Unit(code?.Trim().ToLowerInvariant());

        return UnitValidator.Validate(candidate);
    }
}