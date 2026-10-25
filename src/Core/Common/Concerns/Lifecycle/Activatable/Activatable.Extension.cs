namespace Core.Common.Concerns.Lifecycle.Activatable;

/// <summary>Extension methods for <see cref="IActivatable"/> entities.</summary>
public static class ActivatableExtensions
{
    #region Public Methods

    /// <summary>
    /// Activates an activatable entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> Activate<TValue>(
        this Result<TValue> result,
        DateTimeOffset nowUtc,
        string? actor = null,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : IActivatable
    {
        return result
            .Bind(entity =>
                ActivatableValidator.ValidateActivation(
                    entity,
                    nowUtc,
                    actor,
                    requireActor))

            .Tap(entity =>
            {
                // Compute: Normalize timestamp to UTC and trim actor before mutating.
                entity.IsActive = true;
                entity.ActivatedAtUtc = nowUtc.ToUniversalTime();
                entity.ActivatedBy = actor?.Trim();
            });
    }

    /// <summary>
    /// Deactivates an activatable entity and clears its activation audit metadata.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> Deactivate<TValue>(
        this Result<TValue> result,
        string? actor = null,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : IActivatable
    {
        return result
            .Bind(entity =>
                ActivatableValidator.ValidateDeactivation(
                    entity,
                    actor,
                    requireActor))

            .Tap(entity =>
            {
                // Clear: Reset state and clear activation audit metadata.
                entity.IsActive = false;
                entity.ActivatedAtUtc = null;
                entity.ActivatedBy = null;
            });
    }

    #endregion
}