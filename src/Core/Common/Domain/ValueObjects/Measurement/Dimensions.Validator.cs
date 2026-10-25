using System.Text.RegularExpressions;

namespace Core.Common.Domain.ValueObjects.Measurement;

/// <summary>Validates dimensions without modifying them.</summary>
public static class DimensionsValidator
{
    #region Public Methods

    /// <summary>Validates dimensions without mutating them.</summary>
    /// <summary>Returns success when all measurements are non-negative and the unit code is valid.</summary>
    /// <param name="dimensions">The dimensions to validate.</param>
    /// <returns>The unchanged value on success, or its validation errors.</returns>
    public static Result<Dimensions> Validate(Dimensions dimensions)
    {
        return Result<Dimensions>.Success(dimensions)
            .Ensure(
                predicate: _ =>
                    dimensions.Length >=
                    DimensionsConstant.Constraints.Length.MinValue,
                error: DimensionsOutcome.Failure.LengthNegative)

            .Ensure(
                predicate: _ =>
                    dimensions.Width >=
                    DimensionsConstant.Constraints.Width.MinValue,
                error: DimensionsOutcome.Failure.WidthNegative)

            .Ensure(
                predicate: _ =>
                    dimensions.Height >=
                    DimensionsConstant.Constraints.Height.MinValue,
                error: DimensionsOutcome.Failure.HeightNegative)

            .Ensure(
                predicate: _ =>
                    !string.IsNullOrWhiteSpace(dimensions.Unit.Code) &&
                    Regex.IsMatch(
                        dimensions.Unit.Code!,
                        UnitConstant.Patterns.Code),
                error: DimensionsOutcome.Failure.UnitInvalid);
    }

    #endregion
}