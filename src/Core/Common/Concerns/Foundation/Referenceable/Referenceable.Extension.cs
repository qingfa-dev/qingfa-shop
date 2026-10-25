namespace Core.Common.Concerns.Foundation.Referenceable;

/// <summary>Extension methods for <see cref="IReferenceable"/>.</summary>
public static class ReferenceableExtensions
{
    /// <summary>Sets the entity's reference after validating it.</summary>
    /// <remarks>
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </remarks>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate and mutate.</param>
    /// <param name="reference">The reference value to set; may be <c>null</c>.</param>
    /// <returns>The same result with the reference set; otherwise a failure.</returns>
    public static Result<TValue> SetReference<TValue>(
        this Result<TValue> result,
        string? reference)
        where TValue : IReferenceable
    {
        return result
            .Bind(entity =>
                ReferenceableValidator.ValidateReference(entity, reference))

            .Tap(entity =>
            {
                entity.Reference = reference!;
            });
    }

    /// <summary>Clears the entity's reference back to the empty default.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>The same result with the reference cleared.</returns>
    public static Result<TValue> ClearReference<TValue>(
        this Result<TValue> result)
        where TValue : IReferenceable
    {
        return result.Tap(entity =>
        {
            entity.Reference = ReferenceableConstant.Defaults.Empty;
        });
    }

    /// <summary>Validates that the result's entity carries a usable reference.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The same result if referenced; otherwise a failure.</returns>
    public static Result<TValue> EnsureReferenced<TValue>(
        this Result<TValue> result)
        where TValue : IReferenceable
    {
        return result.Bind(entity =>
            ReferenceableValidator.ValidateReference(entity, entity.Reference));
    }

    /// <summary>Validates the reference and returns the entity, throwing on failure.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The referenced entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public static TValue RequireReferenced<TValue>(
        this Result<TValue> result)
        where TValue : IReferenceable
    {
        return result.EnsureReferenced().ValueOrThrow();
    }
}