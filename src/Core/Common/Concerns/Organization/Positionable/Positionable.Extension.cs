namespace Core.Common.Concerns.Organization.Positionable;

/// <summary>Extension methods for <see cref="IPositionable"/>.</summary>
public static class PositionableExtensions
{
    #region Public Methods

    /// <summary>
    /// Sets the entity's position.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IPositionable"/>.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="position">The position to set.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> SetPosition<TValue>(
        this Result<TValue> result,
        int position)
        where TValue : IPositionable
    {
        return result
            .Bind(entity =>
                PositionableValidator.ValidatePosition(entity, position))
            .Tap(entity =>
            {
                entity.Position = position;
            });
    }

    /// <summary>
    /// Moves the entity by the given offset, clamping validation before mutation.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IPositionable"/>.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="offset">The offset to move by (may be negative).</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> MoveBy<TValue>(
        this Result<TValue> result,
        int offset)
        where TValue : IPositionable
    {
        return result
            .Bind(entity =>
            {
                // Compute: Use long arithmetic so overflow maps to a failure.
                var next = (long)entity.Position + offset;

                if (next < PositionableConstant.Constraints.Position.Min)
                {
                    return Result<TValue>.Fail(
                        PositionableResult.Failure.PositionNegative);
                }

                if (next > PositionableConstant.Constraints.Position.Max)
                {
                    return Result<TValue>.Fail(
                        PositionableResult.Failure.PositionTooLarge);
                }

                return PositionableValidator.ValidatePosition(
                    entity,
                    (int)next);
            })
            .Tap(entity =>
            {
                entity.Position += offset;
            });
    }

    /// <summary>
    /// Moves the entity one step forward.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IPositionable"/>.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> MoveNext<TValue>(
        this Result<TValue> result)
        where TValue : IPositionable
    {
        return result.MoveBy(
            PositionableConstant.Constraints.Position.Step);
    }

    /// <summary>
    /// Moves the entity one step backward.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IPositionable"/>.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> MovePrevious<TValue>(
        this Result<TValue> result)
        where TValue : IPositionable
    {
        return result.MoveBy(
            -PositionableConstant.Constraints.Position.Step);
    }

    /// <summary>
    /// Resets the entity's position to the default.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IPositionable"/>.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ResetPosition<TValue>(
        this Result<TValue> result)
        where TValue : IPositionable
    {
        return result.SetPosition(
            PositionableConstant.Defaults.DefaultPosition);
    }

    #endregion
}