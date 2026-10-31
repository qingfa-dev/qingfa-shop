using System.Text.RegularExpressions;

namespace Core.BuildingBlocks.Domain.ValueObjects.Measurement;

/// <summary>Validates unit codes without modifying them.</summary>
public static class UnitValidator
{
    #region Public Methods

    /// <summary>Validates a unit code without mutating it.</summary>
    /// <summary>Returns success when the code contains one to four lowercase letters.</summary>
    /// <param name="unit">The unit to validate.</param>
    /// <returns>The unchanged unit on success, or its validation errors.</returns>
    public static Result<Unit> Validate(Unit unit)
    {
        return Result<Unit>.Success(unit)
            .Ensure(
                predicate: _ => !string.IsNullOrWhiteSpace(unit.Code),
                error: UnitOutcome.Failure.CodeRequired)

            .Ensure(
                predicate: _ =>
                    unit.Code!.Length <= UnitConstant.Constraints.Code.MaxLength,
                error: UnitOutcome.Failure.CodeTooLong)

            .Ensure(
                predicate: _ =>
                    Regex.IsMatch(unit.Code!, UnitConstant.Patterns.Code),
                error: UnitOutcome.Failure.CodeInvalid);
    }

    #endregion
}