namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.SoftDeletable;

/// <summary>Failure error codes for the SoftDeletable concern.</summary>
public static class SoftDeletableOutcome
{
    /// <summary>Failure error codes for the <see cref="SoftDeletable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Soft-deletable entity is required.");

        /// <summary>Error for when the deletion timestamp is not specified.</summary>
        public static Error DeletedAtRequired => Error.UnprocessableEntity(
            code: Codes.DeletedAtRequired,
            message: "Deleted date must be specified.");

        /// <summary>Error for when the deletion timestamp is too far in the future.</summary>
        public static Error DeletedAtInFuture => Error.UnprocessableEntity(
            code: Codes.DeletedAtInFuture,
            message:
                $"Deleted date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already deleted.</summary>
        public static Error AlreadyDeleted => Error.UnprocessableEntity(
            code: Codes.AlreadyDeleted,
            message: "Entity is already deleted.");

        /// <summary>Error for when the entity is not deleted and cannot be restored.</summary>
        public static Error NotDeleted => Error.UnprocessableEntity(
            code: Codes.NotDeleted,
            message: "Entity is not deleted, so it cannot be restored.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error DeletedByTooLong => Error.UnprocessableEntity(
            code: Codes.DeletedByTooLong,
            message:
                $"Deleted by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error DeletedByTooShort => Error.UnprocessableEntity(
            code: Codes.DeletedByTooShort,
            message:
                $"Deleted by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error DeletedByRequired => Error.UnprocessableEntity(
            code: Codes.DeletedByRequired,
            message: "Deleted by is required.");

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
        public const string EntityRequired = "SoftDeletable.Entity.Required";
        public const string DeletedAtRequired = "SoftDeletable.DeletedAt.Required";
        public const string DeletedAtInFuture = "SoftDeletable.DeletedAt.InFuture";
        public const string AlreadyDeleted = "SoftDeletable.AlreadyDeleted";
        public const string NotDeleted = "SoftDeletable.NotDeleted";
        public const string DeletedByTooLong = "SoftDeletable.DeletedBy.TooLong";
        public const string DeletedByTooShort = "SoftDeletable.DeletedBy.TooShort";
        public const string DeletedByRequired = "SoftDeletable.DeletedBy.Required";
        public const string ActorInvalid = "SoftDeletable.Actor.Invalid";
    }
}