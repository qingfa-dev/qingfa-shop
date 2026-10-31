namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Hierarchical;

/// <summary>Validation logic for <see cref="IHierarchical{TKey}"/>.</summary>
public static class HierarchicalValidator
{
    #region Public Methods

    /// <summary>
    /// Validates the parent assignment that <c>SetParent</c>
    /// is about to apply, without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="parentId">The parent identifier to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateParent<TValue, TKey>(
        TValue auditable,
        TKey? parentId)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                HierarchicalOutcome.Failure.EntityRequired);
        }

        // Guard: Reject empty Guid identifiers; null means root and is valid.
        if (parentId is Guid guid && guid == Guid.Empty)
        {
            return Result<TValue>.Fail(
                HierarchicalOutcome.Failure.ParentEmpty);
        }

        // Compute: Parent assignment is valid for all non-null entities.
        return Result<TValue>.Success(auditable);
    }

    /// <summary>
    /// Validates a parent assignment including a self-parent check,
    /// without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="parentId">The parent identifier to validate.</param>
    /// <param name="entityId">The entity identifier; used to reject self-parent.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateParent<TValue, TKey>(
        TValue auditable,
        TKey? parentId,
        TKey? entityId)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return ValidateParent(auditable, parentId)
            .Ensure(
                predicate: _ => parentId is null
                    || entityId is null
                    || !EqualityComparer<TKey>.Default.Equals(
                        parentId.Value,
                        entityId.Value),
                error: HierarchicalOutcome.Failure.SelfParent);
    }

    /// <summary>
    /// Validates a parent assignment including self-parent and cycle checks,
    /// without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type for the parent identifier.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="parentId">The parent identifier to validate.</param>
    /// <param name="entityId">The entity identifier; used to reject self-parent.</param>
    /// <param name="ancestors">Ancestor identifiers of the proposed parent; used to reject cycles.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateParent<TValue, TKey>(
        TValue auditable,
        TKey? parentId,
        TKey? entityId,
        IEnumerable<TKey> ancestors)
        where TValue : IHierarchical<TKey>
        where TKey : struct
    {
        return ValidateParent(auditable, parentId, entityId)
            .Ensure(
                predicate: _ => parentId is null
                    || !ancestors.Contains(parentId.Value),
                error: HierarchicalOutcome.Failure.CycleDetected)

            // Compute: Reject cycles through the entity id appearing above the parent.
            .Ensure(
                predicate: _ => entityId is null
                    || !ancestors.Contains(entityId.Value),
                error: HierarchicalOutcome.Failure.CycleDetected);
    }

    /// <summary>
    /// Validates a hierarchy depth value against the allowed range,
    /// without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="depth">The depth to validate (root = 0).</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidateDepth<TValue>(
        TValue auditable,
        int depth)
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                HierarchicalOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ =>
                    depth >= HierarchicalConstant.Constraints.Level.Min
                    && depth <= HierarchicalConstant.Constraints.Parent.MaxDepth,
                error: HierarchicalOutcome.Failure.DepthExceeded);
    }

    /// <summary>
    /// Validates a materialized path against length constraints,
    /// without mutating the entity.
    /// </summary>
    /// <typeparam name="TValue">The entity type.</typeparam>
    /// <param name="auditable">The entity to validate.</param>
    /// <param name="path">The materialized path to validate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result<TValue> ValidatePath<TValue>(
        TValue auditable,
        string? path)
    {
        // Guard: Reject null entity before any validation.
        if (auditable is null)
        {
            return Result<TValue>.Fail(
                HierarchicalOutcome.Failure.EntityRequired);
        }

        return Result<TValue>.Success(auditable)
            .Ensure(
                predicate: _ => path is null
                    || path.Length
                        <= HierarchicalConstant.Constraints.Parent.MaxPathLength,
                error: HierarchicalOutcome.Failure.PathTooLong);
    }

    #endregion
}