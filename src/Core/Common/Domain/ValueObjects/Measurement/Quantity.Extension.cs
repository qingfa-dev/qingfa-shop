namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Provides unit-aware arithmetic for quantities.</summary>
public static class QuantityExtensions
{
    /// <summary>
    /// Adds two quantities of the same unit.
    /// </summary>
    /// <summary>Adds two quantities expressed in the same unit.</summary>
    /// <param name="left">The left operand and result unit.</param>
    /// <param name="right">The quantity to add.</param>
    /// <returns>The sum, or a unit-mismatch error.</returns>
    public static Result<Quantity> Add(
        this Quantity left,
        Quantity right)
    {
        return Result<Quantity>.Success(left)
            .Ensure(
                predicate: _ => left.Unit.Code == right.Unit.Code,
                error: QuantityOutcome.Failure.UnitMismatch)

            .Map(quantity =>
                new Quantity(quantity.Value + right.Value, quantity.Unit));
    }
}