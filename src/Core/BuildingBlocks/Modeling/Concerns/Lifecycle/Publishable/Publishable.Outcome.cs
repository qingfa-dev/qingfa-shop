namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Publishable;

/// <summary>Failure error codes for the Publishable concern.</summary>
public static class PublishableOutcome
{
    /// <summary>Failure error codes for the <see cref="Publishable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Publishable entity is required.");

        /// <summary>Error for when the publication timestamp is not specified.</summary>
        public static Error PublishedAtRequired => Error.UnprocessableEntity(
            code: Codes.PublishedAtRequired,
            message: "Published date must be specified.");

        /// <summary>Error for when the publication timestamp is too far in the future.</summary>
        public static Error PublishedAtInFuture => Error.UnprocessableEntity(
            code: Codes.PublishedAtInFuture,
            message:
                $"Published date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already published.</summary>
        public static Error AlreadyPublished => Error.UnprocessableEntity(
            code: Codes.AlreadyPublished,
            message: "Entity is already published.");

        /// <summary>Error for when the entity is not published and cannot be unpublished.</summary>
        public static Error NotPublished => Error.UnprocessableEntity(
            code: Codes.NotPublished,
            message: "Entity is not published, so it cannot be unpublished.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error PublishedByTooLong => Error.UnprocessableEntity(
            code: Codes.PublishedByTooLong,
            message:
                $"Published by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error PublishedByTooShort => Error.UnprocessableEntity(
            code: Codes.PublishedByTooShort,
            message:
                $"Published by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error PublishedByRequired => Error.UnprocessableEntity(
            code: Codes.PublishedByRequired,
            message: "Published by is required.");

        /// <summary>Error for when the actor format is invalid.</summary>
        public static Error ActorInvalid => Error.UnprocessableEntity(
            code: Codes.ActorInvalid,
            message:
                "Actor must not be empty or contain leading/trailing whitespace.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Publishable.Entity.Required";
        public const string PublishedAtRequired = "Publishable.PublishedAt.Required";
        public const string PublishedAtInFuture = "Publishable.PublishedAt.InFuture";
        public const string AlreadyPublished = "Publishable.AlreadyPublished";
        public const string NotPublished = "Publishable.NotPublished";
        public const string PublishedByTooLong = "Publishable.PublishedBy.TooLong";
        public const string PublishedByTooShort = "Publishable.PublishedBy.TooShort";
        public const string PublishedByRequired = "Publishable.PublishedBy.Required";
        public const string ActorInvalid = "Publishable.Actor.Invalid";
    }
}