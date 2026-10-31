namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Versionable;

/// <summary>Validates version state for <see cref="IVersionable"/> entities.</summary>
public static class VersionableValidator
{
    /// <summary>Validates that the entity's version can be incremented,
    /// without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>Success if the version can be incremented; otherwise a failure.</returns>
    public static Result<TValue> ValidateBump<TValue>(
        TValue auditable)
        where TValue : IVersionable
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                VersionableOutcome.Failure.EntityRequired);
        }

        // Compute: Validate the version is not negative.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity =>
                    entity.Version >= VersionableConstant.Constraints.Version.Min,
                error: VersionableOutcome.Failure.VersionNegative)

            // Compute: Validate the version has room left for one increment.
            .Ensure(
                predicate: entity =>
                    entity.Version <= VersionableConstant.Constraints.Version.Max
                        - VersionableConstant.Defaults.Increment,
                error: VersionableOutcome.Failure.VersionOverflow);
    }

    /// <summary>Validates a candidate version value against the allowed range,
    /// without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="version">The version value to validate.</param>
    /// <returns>Success if in range; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateVersion<TValue>(
        TValue auditable,
        long version)
        where TValue : IVersionable
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                VersionableOutcome.Failure.EntityRequired);
        }

        // Compute: Validate the candidate falls within the allowed range.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ =>
                    version >= VersionableConstant.Constraints.Version.Min
                    && version <= VersionableConstant.Constraints.Version.Max,
                error: VersionableOutcome.Failure.VersionOutOfRange);
    }

    /// <summary>Validates optimistic concurrency by comparing the entity's version
    /// against the expected version, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="expectedVersion">The version the caller expects.</param>
    /// <returns>Success on a match; otherwise a concurrency failure.</returns>
    public static Result<TValue> ValidateConcurrency<TValue>(
        TValue auditable,
        long expectedVersion)
        where TValue : IVersionable
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                VersionableOutcome.Failure.EntityRequired);
        }

        // Compute: Validate the versions match, reporting both sides on conflict.
        if (auditable.Version != expectedVersion)
        {
            return Result<TValue>.Fail(
                VersionableOutcome.Failure.ConcurrencyConflict(
                    expectedVersion,
                    auditable.Version));
        }

        return Result<TValue>.Success(auditable);
    }
}