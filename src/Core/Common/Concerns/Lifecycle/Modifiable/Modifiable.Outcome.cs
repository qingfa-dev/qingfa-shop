namespace Core.Common.Concerns.Lifecycle.Modifiable;

/// <summary>Failure error codes for the Modifiable concern.</summary>
public static class ModifiableOutcome
{
    /// <summary>Failure error codes for the <see cref="Modifiable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Modifiable.Entity.Required",
            message: "Modifiable entity is required.");

        /// <summary>Error for when the entity has no creation audit metadata.</summary>
        public static Error NotInitialized => Error.UnprocessableEntity(
            code: "Modifiable.NotInitialized",
            message:
                "Entity has no creation audit metadata; initialize it before marking it modified.");

        /// <summary>Error for when the modification timestamp is not specified.</summary>
        public static Error ModifiedAtRequired => Error.UnprocessableEntity(
            code: "Modifiable.ModifiedAt.Required",
            message: "Modified date must be specified.");

        /// <summary>Error for when the modification timestamp is too far in the future.</summary>
        public static Error ModifiedAtInFuture => Error.UnprocessableEntity(
            code: "Modifiable.ModifiedAt.InFuture",
            message:
                $"Modified date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the modification timestamp is before the creation timestamp.</summary>
        public static Error ModifiedAtBeforeCreatedAt => Error.UnprocessableEntity(
            code: "Modifiable.ModifiedAt.InvalidRange",
            message: "Modified date cannot be earlier than created date.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error ModifiedByTooLong => Error.UnprocessableEntity(
            code: "Modifiable.ModifiedBy.TooLong",
            message:
                $"Modified by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error ModifiedByTooShort => Error.UnprocessableEntity(
            code: "Modifiable.ModifiedBy.TooShort",
            message:
                $"Modified by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error ModifiedByRequired => Error.UnprocessableEntity(
            code: "Modifiable.ModifiedBy.Required",
            message: "Modified by is required when the entity has been modified.");

        /// <summary>Error for when the actor format is invalid.</summary>
        public static Error ActorInvalid => Error.UnprocessableEntity(
            code: "Modifiable.Actor.Invalid",
            message:
                "Actor must not be empty or contain leading/trailing whitespace.");

        #endregion
    }
}