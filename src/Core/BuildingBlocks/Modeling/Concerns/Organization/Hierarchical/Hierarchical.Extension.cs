namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Hierarchical;

/// <summary>Extension methods for <see cref="IHierarchical{TKey}"/>.</summary>
public static class HierarchicalExtensions
{
    #region Public Methods

    /// <summary>
    /// Sets the entity's parent to the given id.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IHierarchical{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="parentId">The parent identifier to set.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> SetParent<TValue, TKey>(
        this Result<TValue> result,
        TKey parentId)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return result.SetParent<TValue, TKey>((TKey?)parentId);
    }

    /// <summary>
    /// Sets the entity's parent; pass <c>null</c> to make it a root.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IHierarchical{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="parentId">The parent identifier to set, or <c>null</c> for root.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> SetParent<TValue, TKey>(
        this Result<TValue> result,
        TKey? parentId)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return result
            .Bind(entity =>
                HierarchicalValidator.ValidateParent<TValue, TKey>(
                    entity,
                    parentId))

            .Tap(entity =>
            {
                entity.ParentId = parentId;
            });
    }

    /// <summary>
    /// Moves the entity under a new parent, rejecting self-parent assignments.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IHierarchical{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="parentId">The new parent identifier, or <c>null</c> for root.</param>
    /// <param name="entityId">The entity identifier; used to reject self-parent.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> Move<TValue, TKey>(
        this Result<TValue> result,
        TKey? parentId,
        TKey? entityId)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return result
            .Bind(entity =>
                HierarchicalValidator.ValidateParent<TValue, TKey>(
                    entity,
                    parentId,
                    entityId))

            .Tap(entity =>
            {
                entity.ParentId = parentId;
            });
    }

    /// <summary>
    /// Reparents the entity, rejecting self-parent assignments and cycles.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IHierarchical{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <param name="parentId">The new parent identifier, or <c>null</c> for root.</param>
    /// <param name="entityId">The entity identifier; used to reject self-parent.</param>
    /// <param name="ancestors">Ancestor identifiers of the proposed parent; used to reject cycles.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> Reparent<TValue, TKey>(
        this Result<TValue> result,
        TKey? parentId,
        TKey? entityId,
        IEnumerable<TKey> ancestors)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return result
            .Bind(entity =>
                HierarchicalValidator.ValidateParent<TValue, TKey>(
                    entity,
                    parentId,
                    entityId,
                    ancestors))

            .Tap(entity =>
            {
                entity.ParentId = parentId;
            });
    }

    /// <summary>
    /// Makes the entity a root by clearing its parent.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    /// <typeparam name="TValue">The entity type implementing <see cref="IHierarchical{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="result">The result carrying the entity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ClearParent<TValue, TKey>(
        this Result<TValue> result)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return result.SetParent<TValue, TKey>((TKey?)null);
    }

    #endregion
}