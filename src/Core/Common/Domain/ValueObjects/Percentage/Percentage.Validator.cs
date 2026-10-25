namespace Core.Common.Domain.ValueObjects.Percentage;

/// <summary>Validates percentages without modifying them.</summary>
public static class PercentageValidator
{
    /// <summary>
    /// Validates a percentage without mutating it.
    /// </summary>
    /// <summary>Returns success when the value is between zero and one hundred inclusive.</summary>
    /// <param name="percentage">The percentage to validate.</param>
    /// <returns>The unchanged percentage on success, or its validation error.</returns>
    public static Result<Percentage> Validate(Percentage percentage)
    {
        return Result<Percentage>.Success(percentage)
            .Ensure(
                predicate: _ =>
                    percentage.Value >=
                        PercentageConstant.Constraints.Value.MinValue &&
                    percentage.Value <=
                        PercentageConstant.Constraints.Value.MaxValue,
                error: PercentageOutcome.Failure.ValueOutOfRange);
    }
}