namespace Core.Common.Concerns.Foundation.Versionable;

/// <summary>Extension methods for <see cref="IVersionable"/>.</summary>
public static class VersionableExtensions
{
    /// <summary>Increments the entity's version by one after validating it.</summary>
    /// <remarks>
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </remarks>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate and mutate.</param>
    /// <returns>The same result with the version incremented; otherwise a failure.</returns>
    public static Result<TValue> BumpVersion<TValue>(
        this Result<TValue> result)
        where TValue : IVersionable
    {
        return result
            .Bind(entity => VersionableValidator.ValidateBump(entity))

            .Tap(entity =>
            {
                entity.Version += VersionableConstant.Defaults.Increment;
            });
    }

    /// <summary>Resets the entity's version to the initial default.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>The same result with the version reset.</returns>
    public static Result<TValue> InitializeVersion<TValue>(
        this Result<TValue> result)
        where TValue : IVersionable
    {
        return result.Tap(entity =>
        {
            entity.Version = VersionableConstant.Defaults.InitialVersion;
        });
    }

    /// <summary>Validates that the result's entity carries a version within the allowed range.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The same result if versioned; otherwise a failure.</returns>
    public static Result<TValue> EnsureVersioned<TValue>(
        this Result<TValue> result)
        where TValue : IVersionable
    {
        return result.Bind(entity =>
            VersionableValidator.ValidateVersion(entity, entity.Version));
    }

    /// <summary>Validates optimistic concurrency against the expected version.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="expectedVersion">The version the caller expects.</param>
    /// <returns>The same result on a match; otherwise a concurrency failure.</returns>
    public static Result<TValue> EnsureVersionMatch<TValue>(
        this Result<TValue> result,
        long expectedVersion)
        where TValue : IVersionable
    {
        return result.Bind(entity =>
            VersionableValidator.ValidateConcurrency(entity, expectedVersion));
    }

    /// <summary>Validates the version and returns the entity, throwing on failure.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The versioned entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public static TValue RequireVersioned<TValue>(
        this Result<TValue> result)
        where TValue : IVersionable
    {
        return result.EnsureVersioned().ValueOrThrow();
    }
}