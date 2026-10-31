namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.StoreScoped;

/// <summary>Extension methods for <see cref="IStoreScoped{TStoreKey}"/>.</summary>
public static class StoreScopedExtensions
{
    /// <summary>Validates that the result's entity carries a non-default store identifier.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The same result if store-scoped; otherwise a failure.</returns>
    public static Result<TValue> EnsureStoreScoped<TValue, TStoreKey>(
        this Result<TValue> result)
        where TValue : IStoreScoped<TStoreKey>
    {
        return result.Bind(entity =>
            StoreScopedValidator.ValidateStoreScope<TValue, TStoreKey>(entity));
    }

    /// <summary>Sets the entity's store identifier after validating it.</summary>
    /// <remarks>
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </remarks>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
    /// <param name="result">The result to validate and mutate.</param>
    /// <param name="storeId">The store identifier value to set.</param>
    /// <returns>The same result with the store identifier set; otherwise a failure.</returns>
    public static Result<TValue> SetStoreId<TValue, TStoreKey>(
        this Result<TValue> result,
        TStoreKey storeId)
        where TValue : IStoreScoped<TStoreKey>
    {
        return result
            .Bind(entity =>
                StoreScopedValidator.ValidateStoreId(entity, storeId))

            .Tap(entity =>
            {
                entity.StoreId = storeId;
            });
    }

    /// <summary>Validates the store scope and returns the entity, throwing on failure.</summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TStoreKey">The type of the store identifier.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <returns>The store-scoped entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public static TValue RequireStoreScoped<TValue, TStoreKey>(
        this Result<TValue> result)
        where TValue : IStoreScoped<TStoreKey>
    {
        return result.EnsureStoreScoped<TValue, TStoreKey>().ValueOrThrow();
    }
}