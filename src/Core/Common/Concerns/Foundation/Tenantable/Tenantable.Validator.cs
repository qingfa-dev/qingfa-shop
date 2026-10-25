namespace Core.Common.Concerns.Foundation.Tenantable;

/// <summary>Validates tenancy state for <see cref="ITenantable{TTenantKey}"/> entities.</summary>
public static class TenantableValidator
{
    /// <summary>Validates that the entity carries a non-default tenant identifier.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>Success if tenanted; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateTenancy<TValue, TTenantKey>(
        TValue auditable)
        where TValue : ITenantable<TTenantKey>
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                TenantableOutcome.Failure.EntityRequired);
        }

        // Compute: Check that the tenant identifier is not the default value for its type.
        // Fallback: Reject Guid.Empty explicitly and blank strings, which a bare
        // default-value comparison does not always cover (string.Empty != default(string)).
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity =>
                    !EqualityComparer<TTenantKey>.Default.Equals(
                        entity.TenantId,
                        default!)
                    && !IsEmptyTenantId(entity.TenantId),
                error: TenantableOutcome.Failure.TenantIdRequired);
    }

    /// <summary>Validates the tenant identifier that
    /// <see cref="TenantableExtensions.SetTenantId{TValue, TTenantKey}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="tenantId">The tenant identifier value to validate.</param>
    /// <returns>Success if usable; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateTenantId<TValue, TTenantKey>(
        TValue auditable,
        TTenantKey tenantId)
        where TValue : ITenantable<TTenantKey>
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                TenantableOutcome.Failure.EntityRequired);
        }

        // Compute: Check the candidate is neither the default nor an empty sentinel.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ =>
                    tenantId is not null
                    && !EqualityComparer<TTenantKey>.Default.Equals(
                        tenantId,
                        default!)
                    && !IsEmptyTenantId(tenantId),
                error: TenantableOutcome.Failure.TenantIdRequired);
    }

    /// <summary>Checks <see cref="Guid.Empty"/> and blank-string sentinels for the tenant identifier.</summary>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="tenantId">The tenant identifier value to inspect.</param>
    /// <returns><c>true</c> when the value is an empty sentinel; otherwise <c>false</c>.</returns>
    private static bool IsEmptyTenantId<TTenantKey>(
        TTenantKey tenantId) =>
        (tenantId is Guid guid && guid == TenantableConstant.Defaults.EmptyTenantId)
        || (tenantId is string text && string.IsNullOrWhiteSpace(text));
}