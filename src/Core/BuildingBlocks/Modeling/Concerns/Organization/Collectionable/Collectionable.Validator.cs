namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Collectionable;

/// <summary>Validation logic for <see cref="ICollectionable{TCollection}"/>.</summary>
public static class CollectionableValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the collection that
    /// <see cref="CollectionableExtensions.AddCollection{TValue, TCollection}"/>
    /// is about to add, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="collection">The collection to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateAddCollection<
        TValue,
        TCollection>(
        TValue auditable,
        TCollection collection)
        where TValue : ICollectionable<TCollection>
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                CollectionableOutcome.Failure.EntityRequired);
        }

        // Guard: Reject null collection before any validation.
        if (collection is null)
        {
            return Result<TValue>.Fail(
                CollectionableOutcome.Failure.CollectionRequired);
        }

        // Guard: Reject empty Guid identifiers before any validation.
        if (collection is Guid guid && guid == Guid.Empty)
        {
            return Result<TValue>.Fail(
                CollectionableOutcome.Failure.CollectionEmpty);
        }

        // Compute: Normalize string collections for length and duplicate checks.
        var normalized = Normalize(collection);

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => normalized is not string name
                    || !string.IsNullOrWhiteSpace(name),
                error: CollectionableOutcome.Failure.CollectionRequired)

            .Ensure(
                predicate: _ => normalized is not string name
                    || name.Length
                        <= CollectionableConstant.Constraints.Collection.MaxNameLength,
                error: CollectionableOutcome.Failure.CollectionTooLong)

            // Compute: Ensure the collection is not already assigned.
            .Ensure(
                predicate: entity => !entity.Collections.Contains(normalized),
                error: CollectionableOutcome.Failure.CollectionDuplicate)

            // Fallback: Ensure the entity has room for another collection.
            .Ensure(
                predicate: entity =>
                    entity.Collections.Count
                        < CollectionableConstant.Constraints.Collection.MaxCount,
                error: CollectionableOutcome.Failure.CollectionLimitExceeded);
    }

    /// <summary>
    /// Validates that the collection that
    /// <see cref="CollectionableExtensions.RemoveCollection{TValue, TCollection}"/>
    /// targets is assigned, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="collection">The collection to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateRemoveCollection<
        TValue,
        TCollection>(
        TValue auditable,
        TCollection collection)
        where TValue : ICollectionable<TCollection>
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                CollectionableOutcome.Failure.EntityRequired);
        }

        // Guard: Reject null collection before any validation.
        if (collection is null)
        {
            return Result<TValue>.Fail(
                CollectionableOutcome.Failure.CollectionRequired);
        }

        // Compute: Normalize string collections so padded input still matches.
        var normalized = Normalize(collection);

        // Compute: Ensure the collection is currently assigned.
        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: entity => entity.Collections.Contains(normalized),
                error: CollectionableOutcome.Failure.CollectionNotFound);
    }

    #endregion

    #region Internal Methods

    /// <summary>Normalizes a collection value (trims strings when enabled).</summary>
    internal static TCollection Normalize<TCollection>(TCollection collection)
    {
        if (CollectionableConstant.Defaults.TrimValues
            && collection is string name)
        {
            return (TCollection)(object)name.Trim();
        }

        return collection;
    }

    #endregion
}