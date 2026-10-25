using Core.Common.Concerns.Lifecycle.Creatable;

namespace Core.Common.Concerns.Lifecycle.Modifiable;

/// <summary>Extension methods for <see cref="IModifiable"/> entities.</summary>
public static class ModifiableExtensions
{
    #region Public Methods

    /// <summary>
    /// Updates modification audit metadata for a modified entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> MarkModified<TValue>(
        this Result<TValue> result,
        DateTimeOffset nowUtc,
        string? actor = null,
        bool requireModifiedBy = LifecycleConstant.Defaults.RequireActor)
        where TValue : ICreatable, IModifiable
    {
        return result
            .Bind(entity =>
                ModifiableValidator.ValidateModification(
                    entity,
                    nowUtc,
                    actor,
                    requireModifiedBy))

            .Tap(entity =>
            {
                // Compute: Normalize timestamp to UTC and trim actor before mutating.
                entity.ModifiedAtUtc = nowUtc.ToUniversalTime();
                entity.ModifiedBy = actor?.Trim();
            });
    }

    #endregion
}