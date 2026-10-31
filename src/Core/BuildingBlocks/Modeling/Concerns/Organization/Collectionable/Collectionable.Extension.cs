namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Collectionable;

/// <summary>Extension methods for <see cref="ICollectionable{TCollection}"/>.</summary>
public static class CollectionableExtensions
{
    #region Public Methods

    /// <summary>
    /// Adds a collection to the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// String collections are trimmed before validation.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICollectionable{TCollection}"/>.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="collection">The collection to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> AddCollection<TValue, TCollection>(
        this Result<TValue> result,
        TCollection collection)
        where TValue : ICollectionable<TCollection>
    {
        // Compute: Normalize input so padded strings match stored values.
        var normalized = CollectionableValidator.Normalize(collection);

        return result
            .Bind(entity =>
                CollectionableValidator.ValidateAddCollection(
                    entity,
                    normalized))

            .Tap(entity =>
            {
                entity.Collections.Add(normalized);
            });
    }

    /// <summary>
    /// Adds multiple collections to the entity atomically.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICollectionable{TCollection}"/>.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="collections">The collections to append.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> AddCollections<TValue, TCollection>(
        this Result<TValue> result,
        IEnumerable<TCollection> collections)
        where TValue : ICollectionable<TCollection>
    {
        // Compute: Normalize the batch once so validation and mutation agree.
        var normalized = collections
            .Select(CollectionableValidator.Normalize)
            .ToList();

        return result
            .Bind(entity =>
            {
                // Guard: Validate the whole batch before mutating anything.
                var validation = Result<TValue>.Success(entity);

                foreach (var collection in normalized)
                {
                    validation = validation.Bind(current =>
                        CollectionableValidator.ValidateAddCollection(
                            current,
                            collection));

                    if (validation.IsFailure)
                    {
                        return validation;
                    }
                }

                // Fallback: Ensure the batch fits within the max count.
                if (entity.Collections.Count + normalized.Count
                    > CollectionableConstant.Constraints.Collection.MaxCount)
                {
                    return Result<TValue>.Fail(
                        CollectionableOutcome.Failure.CollectionLimitExceeded);
                }

                return validation;
            })

            .Tap(entity =>
            {
                foreach (var collection in normalized)
                {
                    entity.Collections.Add(collection);
                }
            });
    }

    /// <summary>
    /// Removes a collection from the entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICollectionable{TCollection}"/>.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="collection">The collection to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> RemoveCollection<TValue, TCollection>(
        this Result<TValue> result,
        TCollection collection)
        where TValue : ICollectionable<TCollection>
    {
        // Compute: Normalize input so padded strings match stored values.
        var normalized = CollectionableValidator.Normalize(collection);

        return result
            .Bind(entity =>
                CollectionableValidator.ValidateRemoveCollection(
                    entity,
                    normalized))

            .Tap(entity =>
            {
                entity.Collections.Remove(normalized);
            });
    }

    /// <summary>
    /// Replaces all collections with the given set.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICollectionable{TCollection}"/>.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="collections">The replacement collections.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> SetCollections<TValue, TCollection>(
        this Result<TValue> result,
        IEnumerable<TCollection> collections)
        where TValue : ICollectionable<TCollection>
    {
        // Compute: Normalize and de-duplicate the incoming set.
        var normalized = collections
            .Select(CollectionableValidator.Normalize)
            .Distinct()
            .ToList();

        return result
            .Bind(entity =>
            {
                // Guard: Reject null entity before any validation.
                if (entity is null)
                {
                    return Result<TValue>.Fail(
                        CollectionableOutcome.Failure.EntityRequired);
                }

                // Fallback: Ensure the replacement fits within the max count.
                if (normalized.Count
                    > CollectionableConstant.Constraints.Collection.MaxCount)
                {
                    return Result<TValue>.Fail(
                        CollectionableOutcome.Failure.CollectionLimitExceeded);
                }

                return Result<TValue>.Success(entity);
            })

            .Tap(entity =>
            {
                entity.Collections.Clear();

                foreach (var collection in normalized)
                {
                    entity.Collections.Add(collection);
                }
            });
    }

    /// <summary>
    /// Removes all collections from the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="ICollectionable{TCollection}"/>.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ClearCollections<TValue, TCollection>(
        this Result<TValue> result)
        where TValue : ICollectionable<TCollection>
    {
        return result.Tap(entity =>
        {
            entity.Collections.Clear();
        });
    }

    #endregion
}