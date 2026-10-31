namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>A non-negative weight expressed in a unit.</summary>
/// <param name="Value">The weight amount.</param>
/// <param name="Unit">The unit of the amount.</param>
public readonly partial record struct Weight(decimal Value, Unit Unit)
{
    /// <summary>Creates a weight when its amount is non-negative and its unit is valid.</summary>
    /// <param name="value">The weight amount.</param>
    /// <param name="unit">The unit of the amount.</param>
    /// <returns>The weight, or validation errors.</returns>
    public static Result<Weight> Create(
        decimal value,
        Unit unit)
    {
        var candidate = new Weight(value, unit);

        return WeightValidator.Validate(candidate);
    }
}