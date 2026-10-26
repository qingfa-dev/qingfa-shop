namespace Core.Common.Concerns.Foundation.Tenantable;

/// <summary>Extension methods for <see cref="ITenantable{TTenantKey}"/>.</summary>
public static class TenantableExtensions
{
    /// <summary>Validates that the result's entity carries a non-default tenant identifier.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The same result if tenanted; otherwise a failure.</returns>
    public static Result<TValue> EnsureTenanted<TValue, TTenantKey>(
        this Result<TValue> result)
        where TValue : ITenantable<TTenantKey>
    {
        return result.Bind(entity =>
            TenantableValidator.ValidateTenancy<TValue, TTenantKey>(entity));
    }

    /// <summary>Sets the entity's tenant identifier after validating it.</summary>
    /// <remarks>
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </remarks>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="result">The result to validate and mutate.</param>
    /// <param name="tenantId">The tenant identifier value to set.</param>
    /// <returns>The same result with the tenant identifier set; otherwise a failure.</returns>
    public static Result<TValue> SetTenantId<TValue, TTenantKey>(
        this Result<TValue> result,
        TTenantKey tenantId)
        where TValue : ITenantable<TTenantKey>
    {
        return result
            .Bind(entity =>
                TenantableValidator.ValidateTenantId(entity, tenantId))

            .Tap(entity =>
            {
                entity.TenantId = tenantId;
            });
    }

    /// <summary>Validates the tenancy and returns the entity, throwing on failure.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The tenanted entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public static TValue RequireTenanted<TValue, TTenantKey>(
        this Result<TValue> result)
        where TValue : ITenantable<TTenantKey>
    {
        return result.EnsureTenanted<TValue, TTenantKey>().ValueOrThrow();
    }
}