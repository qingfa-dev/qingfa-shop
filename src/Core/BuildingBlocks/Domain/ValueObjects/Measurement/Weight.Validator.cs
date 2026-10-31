using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Validates weights without modifying them.</summary>
public static class WeightValidator
{
    #region Public Methods

    /// <summary>Validates a weight without mutating it.</summary>
    /// <summary>Returns success when the amount is non-negative and its unit code is valid.</summary>
    /// <param name="weight">The weight to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<Weight> Validate(Weight weight)
    {
        return Result<Weight>.Success(weight)
            .Ensure(
                predicate: _ =>
                    weight.Value >= WeightConstant.Constraints.Value.MinValue,
                error: WeightOutcome.Failure.ValueNegative)

            .Ensure(
                predicate: _ =>
                    !string.IsNullOrWhiteSpace(weight.Unit.Code) &&
                    Regex.IsMatch(
                        weight.Unit.Code!,
                        UnitConstant.Patterns.Code),
                error: WeightOutcome.Failure.UnitInvalid);
    }

    #endregion
}