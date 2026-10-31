namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Activatable;

/// <summary>Failure error codes for the Activatable concern.</summary>
public static class ActivatableOutcome
{
    /// <summary>Failure error codes for the <see cref="Activatable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Activatable entity is required.");

        /// <summary>Error for when the activation timestamp is not specified.</summary>
        public static Error ActivatedAtRequired => Error.UnprocessableEntity(
            code: Codes.ActivatedAtRequired,
            message: "Activated date must be specified.");

        /// <summary>Error for when the activation timestamp is too far in the future.</summary>
        public static Error ActivatedAtInFuture => Error.UnprocessableEntity(
            code: Codes.ActivatedAtInFuture,
            message:
                $"Activated date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already active.</summary>
        public static Error AlreadyActive => Error.UnprocessableEntity(
            code: Codes.AlreadyActive,
            message: "Entity is already active.");

        /// <summary>Error for when the entity is not active and cannot be deactivated.</summary>
        public static Error NotActive => Error.UnprocessableEntity(
            code: Codes.NotActive,
            message: "Entity is not active, so it cannot be deactivated.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error ActivatedByTooLong => Error.UnprocessableEntity(
            code: Codes.ActivatedByTooLong,
            message:
                $"Activated by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error ActivatedByTooShort => Error.UnprocessableEntity(
            code: Codes.ActivatedByTooShort,
            message:
                $"Activated by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error ActivatedByRequired => Error.UnprocessableEntity(
            code: Codes.ActivatedByRequired,
            message: "Activated by is required.");

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
        public const string EntityRequired = "Activatable.Entity.Required";
        public const string ActivatedAtRequired = "Activatable.ActivatedAt.Required";
        public const string ActivatedAtInFuture = "Activatable.ActivatedAt.InFuture";
        public const string AlreadyActive = "Activatable.AlreadyActive";
        public const string NotActive = "Activatable.NotActive";
        public const string ActivatedByTooLong = "Activatable.ActivatedBy.TooLong";
        public const string ActivatedByTooShort = "Activatable.ActivatedBy.TooShort";
        public const string ActivatedByRequired = "Activatable.ActivatedBy.Required";
        public const string ActorInvalid = "Activatable.Actor.Invalid";
    }
}