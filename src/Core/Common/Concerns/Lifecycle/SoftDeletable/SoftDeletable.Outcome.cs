namespace Core.Common.Concerns.Lifecycle.SoftDeletable;

/// <summary>Failure error codes for the SoftDeletable concern.</summary>
public static class SoftDeletableOutcome
{
    /// <summary>Failure error codes for the <see cref="SoftDeletable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "SoftDeletable.Entity.Required",
            message: "Soft-deletable entity is required.");

        /// <summary>Error for when the deletion timestamp is not specified.</summary>
        public static Error DeletedAtRequired => Error.UnprocessableEntity(
            code: "SoftDeletable.DeletedAt.Required",
            message: "Deleted date must be specified.");

        /// <summary>Error for when the deletion timestamp is too far in the future.</summary>
        public static Error DeletedAtInFuture => Error.UnprocessableEntity(
            code: "SoftDeletable.DeletedAt.InFuture",
            message:
                $"Deleted date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already deleted.</summary>
        public static Error AlreadyDeleted => Error.UnprocessableEntity(
            code: "SoftDeletable.AlreadyDeleted",
            message: "Entity is already deleted.");

        /// <summary>Error for when the entity is not deleted and cannot be restored.</summary>
        public static Error NotDeleted => Error.UnprocessableEntity(
            code: "SoftDeletable.NotDeleted",
            message: "Entity is not deleted, so it cannot be restored.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error DeletedByTooLong => Error.UnprocessableEntity(
            code: "SoftDeletable.DeletedBy.TooLong",
            message:
                $"Deleted by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error DeletedByTooShort => Error.UnprocessableEntity(
            code: "SoftDeletable.DeletedBy.TooShort",
            message:
                $"Deleted by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error DeletedByRequired => Error.UnprocessableEntity(
            code: "SoftDeletable.DeletedBy.Required",
            message: "Deleted by is required.");

        /// <summary>Error for when the actor format is invalid.</summary>
        public static Error ActorInvalid => Error.UnprocessableEntity(
            code: "SoftDeletable.Actor.Invalid",
            message:
                "Actor must not be empty or contain leading/trailing whitespace.");

        #endregion
    }
}