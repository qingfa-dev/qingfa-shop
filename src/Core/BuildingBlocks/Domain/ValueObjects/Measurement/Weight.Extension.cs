namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Provides unit-aware arithmetic for weights.</summary>
public static class WeightExtensions
{
    /// <summary>
    /// Adds two weights of the same unit.
    /// </summary>
    /// <summary>Adds two weights expressed in the same unit.</summary>
    /// <param name="left">The left operand and result unit.</param>
    /// <param name="right">The weight to add.</param>
    /// <returns>The sum, or a unit-mismatch error.</returns>
    public static Result<Weight> Add(
        this Weight left,
        Weight right)
    {
        return Result<Weight>.Success(left)
            .Ensure(
                predicate: _ => left.Unit.Code == right.Unit.Code,
                error: WeightOutcome.Failure.UnitMismatch)

            .Map(weight =>
                new Weight(weight.Value + right.Value, weight.Unit));
    }
}