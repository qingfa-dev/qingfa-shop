namespace Core.Common.Concerns.Lifecycle.SoftDeletable;

/// <summary>Extension methods for <see cref="ISoftDeletable"/> entities.</summary>
public static class SoftDeletableExtensions
{
    #region Public Methods

    /// <summary>
    /// Marks a soft-deletable entity as deleted.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> MarkDeleted<TValue>(
        this Result<TValue> result,
        DateTimeOffset nowUtc,
        string? actor = null,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : ISoftDeletable
    {
        return result
            .Bind(entity =>
                SoftDeletableValidator.ValidateDeletion(
                    entity,
                    nowUtc,
                    actor,
                    requireActor))

            .Tap(entity =>
            {
                // Compute: Normalize timestamp to UTC and trim actor before mutating.
                entity.IsDeleted = true;
                entity.DeletedAtUtc = nowUtc.ToUniversalTime();
                entity.DeletedBy = actor?.Trim();
            });
    }

    /// <summary>
    /// Restores a soft-deletable entity and clears its deletion audit metadata.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> Restore<TValue>(
        this Result<TValue> result,
        string? actor = null,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : ISoftDeletable
    {
        return result
            .Bind(entity =>
                SoftDeletableValidator.ValidateRestoration(
                    entity,
                    actor,
                    requireActor))

            .Tap(entity =>
            {
                // Clear: Reset state and clear deletion audit metadata.
                entity.IsDeleted = false;
                entity.DeletedAtUtc = null;
                entity.DeletedBy = null;
            });
    }

    #endregion
}