namespace Core.Common.Concerns.Lifecycle.Creatable;

/// <summary>Extension methods for <see cref="ICreatable"/> entities.</summary>
public static class CreatableExtensions
{
    #region Public Methods

    /// <summary>
    /// Initializes creation audit metadata for a newly created entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> InitializeAudit<TValue>(
        this Result<TValue> result,
        DateTimeOffset nowUtc,
        string? actor = null,
        bool requireCreatedBy = LifecycleConstant.Defaults.RequireActor)
        where TValue : ICreatable
    {
        return result
            .Bind(entity =>
                CreatableValidator.ValidateCreation(
                    entity,
                    nowUtc,
                    actor,
                    requireCreatedBy))

            .Tap(entity =>
            {
                // Compute: Normalize timestamp to UTC and trim actor before mutating.
                entity.CreatedAtUtc = nowUtc.ToUniversalTime();
                entity.CreatedBy = actor?.Trim();
            });
    }

    #endregion
}