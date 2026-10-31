namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Publishable;

/// <summary>Extension methods for <see cref="IPublishable"/> entities.</summary>
public static class PublishableExtensions
{
    #region Public Methods

    /// <summary>
    /// Publishes a publishable entity.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> Publish<TValue>(
        this Result<TValue> result,
        DateTimeOffset nowUtc,
        string? actor = null,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : IPublishable
    {
        return result
            .Bind(entity =>
                PublishableValidator.ValidatePublication(
                    entity,
                    nowUtc,
                    actor,
                    requireActor))

            .Tap(entity =>
            {
                // Compute: Normalize timestamp to UTC and trim actor before mutating.
                entity.IsPublished = true;
                entity.PublishedAtUtc = nowUtc.ToUniversalTime();
                entity.PublishedBy = actor?.Trim();
            });
    }

    /// <summary>
    /// Unpublishes a publishable entity and clears its publication audit metadata.
    /// Validation runs before any mutation, so a failure leaves the entity untouched.
    /// </summary>
    public static Result<TValue> Unpublish<TValue>(
        this Result<TValue> result,
        string? actor = null,
        bool requireActor = LifecycleConstant.Defaults.RequireActor)
        where TValue : IPublishable
    {
        return result
            .Bind(entity =>
                PublishableValidator.ValidateUnpublication(
                    entity,
                    actor,
                    requireActor))

            .Tap(entity =>
            {
                // Clear: Reset state and clear publication audit metadata.
                entity.IsPublished = false;
                entity.PublishedAtUtc = null;
                entity.PublishedBy = null;
            });
    }

    #endregion
}