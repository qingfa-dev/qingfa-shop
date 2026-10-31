namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Three non-negative measurements expressed in a single unit.</summary>
/// <param name="Length">The length measurement.</param>
/// <param name="Width">The width measurement.</param>
/// <param name="Height">The height measurement.</param>
/// <param name="Unit">The unit shared by all three measurements.</param>
public readonly partial record struct Dimensions(
    decimal Length,
    decimal Width,
    decimal Height,
    Unit Unit)
{
    /// <summary>Creates dimensions when all measurements are non-negative and the unit is valid.</summary>
    /// <param name="length">The length measurement.</param>
    /// <param name="width">The width measurement.</param>
    /// <param name="height">The height measurement.</param>
    /// <param name="unit">The unit shared by all measurements.</param>
    /// <returns>The dimensions, or validation errors.</returns>
    public static Result<Dimensions> Create(
        decimal length,
        decimal width,
        decimal height,
        Unit unit)
    {
        var candidate = new Dimensions(length, width, height, unit);

        return DimensionsValidator.Validate(candidate);
    }
}