namespace Core.Common.Concerns.Lifecycle.Activatable;

/// <summary>Failure error codes for the Activatable concern.</summary>
public static class ActivatableOutcome
{
    /// <summary>Failure error codes for the <see cref="Activatable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Activatable.Entity.Required",
            message: "Activatable entity is required.");

        /// <summary>Error for when the activation timestamp is not specified.</summary>
        public static Error ActivatedAtRequired => Error.UnprocessableEntity(
            code: "Activatable.ActivatedAt.Required",
            message: "Activated date must be specified.");

        /// <summary>Error for when the activation timestamp is too far in the future.</summary>
        public static Error ActivatedAtInFuture => Error.UnprocessableEntity(
            code: "Activatable.ActivatedAt.InFuture",
            message:
                $"Activated date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already active.</summary>
        public static Error AlreadyActive => Error.UnprocessableEntity(
            code: "Activatable.AlreadyActive",
            message: "Entity is already active.");

        /// <summary>Error for when the entity is not active and cannot be deactivated.</summary>
        public static Error NotActive => Error.UnprocessableEntity(
            code: "Activatable.NotActive",
            message: "Entity is not active, so it cannot be deactivated.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error ActivatedByTooLong => Error.UnprocessableEntity(
            code: "Activatable.ActivatedBy.TooLong",
            message:
                $"Activated by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error ActivatedByTooShort => Error.UnprocessableEntity(
            code: "Activatable.ActivatedBy.TooShort",
            message:
                $"Activated by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error ActivatedByRequired => Error.UnprocessableEntity(
            code: "Activatable.ActivatedBy.Required",
            message: "Activated by is required.");

        /// <summary>Error for when the actor format is invalid.</summary>
        public static Error ActorInvalid => Error.UnprocessableEntity(
            code: "Activatable.Actor.Invalid",
            message:
                "Actor must not be empty or contain leading/trailing whitespace.");

        #endregion
    }
}