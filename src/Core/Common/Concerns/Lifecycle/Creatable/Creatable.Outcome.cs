namespace Core.Common.Concerns.Lifecycle.Creatable;

/// <summary>Failure error codes for the Creatable concern.</summary>
public static class CreatableOutcome
{
    /// <summary>Failure error codes for the <see cref="Creatable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Creatable.Entity.Required",
            message: "Creatable entity is required.");

        /// <summary>Error for when the creation timestamp is not specified.</summary>
        public static Error CreatedAtRequired => Error.UnprocessableEntity(
            code: "Creatable.CreatedAt.Required",
            message: "Created date must be specified.");

        /// <summary>Error for when the creation timestamp is too far in the future.</summary>
        public static Error CreatedAtInFuture => Error.UnprocessableEntity(
            code: "Creatable.CreatedAt.InFuture",
            message:
                $"Created date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already initialized.</summary>
        public static Error AlreadyInitialized => Error.UnprocessableEntity(
            code: "Creatable.CreatedAt.AlreadyInitialized",
            message:
                "Creation audit metadata is already set; it cannot be initialized twice.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error CreatedByTooLong => Error.UnprocessableEntity(
            code: "Creatable.CreatedBy.TooLong",
            message:
                $"Created by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error CreatedByTooShort => Error.UnprocessableEntity(
            code: "Creatable.CreatedBy.TooShort",
            message:
                $"Created by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error CreatedByRequired => Error.UnprocessableEntity(
            code: "Creatable.CreatedBy.Required",
            message: "Created by is required.");

        /// <summary>Error for when the actor format is invalid.</summary>
        public static Error ActorInvalid => Error.UnprocessableEntity(
            code: "Creatable.Actor.Invalid",
            message:
                "Actor must not be empty or contain leading/trailing whitespace.");

        #endregion
    }
}