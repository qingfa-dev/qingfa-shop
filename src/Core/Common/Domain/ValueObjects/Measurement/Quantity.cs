namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>A non-negative quantity expressed in a unit.</summary>
/// <param name="Value">The quantity amount.</param>
/// <param name="Unit">The unit of the amount.</param>
public readonly partial record struct Quantity(decimal Value, Unit Unit)
{
    /// <summary>Creates a quantity when its amount is non-negative and its unit is valid.</summary>
    /// <param name="value">The quantity amount.</param>
    /// <param name="unit">The unit of the amount.</param>
    /// <returns>The quantity, or validation errors.</returns>
    public static Result<Quantity> Create(
        decimal value,
        Unit unit)
    {
        var candidate = new Quantity(value, unit);

        return QuantityValidator.Validate(candidate);
    }
}