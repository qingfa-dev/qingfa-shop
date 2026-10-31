namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.StoreScoped;

/// <summary>Validates store-scoping state for <see cref="IStoreScoped{TStoreKey}"/> entities.</summary>
public static class StoreScopedValidator
{
    /// <summary>Validates that the entity carries a non-default store identifier.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <returns>Success if store-scoped; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateStoreScope<TValue, TStoreKey>(
        TValue auditable)
        where TValue : IStoreScoped<TStoreKey>
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                StoreScopedOutcome.Failure.EntityRequired);
        }

        // Compute: Check that the store identifier is not the default value for its type.
        // Fallback: Reject Guid.Empty explicitly and blank strings, which a bare
        // default-value comparison does not always cover (string.Empty != default(string)).
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity =>
                    !EqualityComparer<TStoreKey>.Default.Equals(
                        entity.StoreId,
                        default!)
                    && !IsEmptyStoreId(entity.StoreId),
                error: StoreScopedOutcome.Failure.StoreIdRequired);
    }

    /// <summary>Validates the store identifier that
    /// <see cref="StoreScopedExtensions.SetStoreId{TValue, TStoreKey}"/>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="storeId">The store identifier value to validate.</param>
    /// <returns>Success if usable; otherwise a failure with the appropriate error.</returns>
    public static Result<TValue> ValidateStoreId<TValue, TStoreKey>(
        TValue auditable,
        TStoreKey storeId)
        where TValue : IStoreScoped<TStoreKey>
    {
        // Guard: Reject null entities immediately.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                StoreScopedOutcome.Failure.EntityRequired);
        }

        // Compute: Check the candidate is neither the default nor an empty sentinel.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ =>
                    storeId is not null
                    && !EqualityComparer<TStoreKey>.Default.Equals(
                        storeId,
                        default!)
                    && !IsEmptyStoreId(storeId),
                error: StoreScopedOutcome.Failure.StoreIdRequired);
    }

    /// <summary>Checks <see cref="Guid.Empty"/> and blank-string sentinels for the store identifier.</summary>
    /// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
    /// <param name="storeId">The store identifier value to inspect.</param>
    /// <returns><c>true</c> when the value is an empty sentinel; otherwise <c>false</c>.</returns>
    private static bool IsEmptyStoreId<TStoreKey>(
        TStoreKey storeId) =>
        (storeId is Guid guid && guid == StoreScopedConstant.Defaults.EmptyStoreId)
        || (storeId is string text && string.IsNullOrWhiteSpace(text));
}