namespace Core.Common.Domain.ValueObjects.Percentage;

/// <summary>A percentage value constrained to the inclusive range zero to one hundred.</summary>
/// <param name="Value">The percentage number, where <c>100</c> represents one hundred percent.</param>
public readonly partial record struct Percentage(decimal Value)
{
    /// <summary>Creates a percentage within the inclusive range zero to one hundred.</summary>
    /// <param name="value">The percentage number.</param>
    /// <returns>The percentage, or a range validation error.</returns>
    public static Result<Percentage> Create(decimal value)
    {
        var candidate = new Percentage(value);

        return PercentageValidator.Validate(candidate);
    }
}