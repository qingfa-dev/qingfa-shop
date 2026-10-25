namespace Core.Common.Concerns.Lifecycle.Publishable;

/// <summary>Failure error codes for the Publishable concern.</summary>
public static class PublishableOutcome
{
    /// <summary>Failure error codes for the <see cref="Publishable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Publishable.Entity.Required",
            message: "Publishable entity is required.");

        /// <summary>Error for when the publication timestamp is not specified.</summary>
        public static Error PublishedAtRequired => Error.UnprocessableEntity(
            code: "Publishable.PublishedAt.Required",
            message: "Published date must be specified.");

        /// <summary>Error for when the publication timestamp is too far in the future.</summary>
        public static Error PublishedAtInFuture => Error.UnprocessableEntity(
            code: "Publishable.PublishedAt.InFuture",
            message:
                $"Published date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already published.</summary>
        public static Error AlreadyPublished => Error.UnprocessableEntity(
            code: "Publishable.AlreadyPublished",
            message: "Entity is already published.");

        /// <summary>Error for when the entity is not published and cannot be unpublished.</summary>
        public static Error NotPublished => Error.UnprocessableEntity(
            code: "Publishable.NotPublished",
            message: "Entity is not published, so it cannot be unpublished.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error PublishedByTooLong => Error.UnprocessableEntity(
            code: "Publishable.PublishedBy.TooLong",
            message:
                $"Published by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error PublishedByTooShort => Error.UnprocessableEntity(
            code: "Publishable.PublishedBy.TooShort",
            message:
                $"Published by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error PublishedByRequired => Error.UnprocessableEntity(
            code: "Publishable.PublishedBy.Required",
            message: "Published by is required.");

        /// <summary>Error for when the actor format is invalid.</summary>
        public static Error ActorInvalid => Error.UnprocessableEntity(
            code: "Publishable.Actor.Invalid",
            message:
                "Actor must not be empty or contain leading/trailing whitespace.");

        #endregion
    }
}