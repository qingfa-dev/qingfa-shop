namespace Core.Common.Concerns.Organization.Positionable;

/// <summary>Validation logic for <see cref="IPositionable"/>.</summary>
public static class PositionableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the position that <see cref="PositionableExtensions.SetPosition{TValue}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="position">The position to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidatePosition<TValue>(
        TValue auditable,
        int position)
        where TValue : IPositionable
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                PositionableResult.Failure.EntityRequired);
        }

        // Compute: Ensure the position is within the allowed range.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ =>
                    position >= PositionableConstant.Constraints.Position.Min,
                error: PositionableResult.Failure.PositionNegative)

            // Fallback: Ensure the position does not exceed the maximum.
            .Ensure(
                predicate: _ =>
                    position <= PositionableConstant.Constraints.Position.Max,
                error: PositionableResult.Failure.PositionTooLarge);
    }

    #endregion
}