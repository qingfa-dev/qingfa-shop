using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Validates quantities without modifying them.</summary>
public static class QuantityValidator
{
    #region Public Methods

    /// <summary>Validates a quantity without mutating it.</summary>
    /// <summary>Returns success when the amount is non-negative and its unit code is valid.</summary>
    /// <param name="quantity">The quantity to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<Quantity> Validate(Quantity quantity)
    {
        return Result<Quantity>.Success(quantity)
            .Ensure(
                predicate: _ =>
                    quantity.Value >= QuantityConstant.Constraints.Value.MinValue,
                error: QuantityOutcome.Failure.ValueNegative)

            .Ensure(
                predicate: _ =>
                    !string.IsNullOrWhiteSpace(quantity.Unit.Code) &&
                    Regex.IsMatch(
                        quantity.Unit.Code!,
                        UnitConstant.Patterns.Code),
                error: QuantityOutcome.Failure.UnitInvalid);
    }

    #endregion
}