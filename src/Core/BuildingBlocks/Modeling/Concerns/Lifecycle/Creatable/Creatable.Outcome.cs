namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;

/// <summary>Failure error codes for the Creatable concern.</summary>
public static class CreatableOutcome
{
    /// <summary>Failure error codes for the <see cref="Creatable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Creatable entity is required.");

        /// <summary>Error for when the creation timestamp is not specified.</summary>
        public static Error CreatedAtRequired => Error.UnprocessableEntity(
            code: Codes.CreatedAtRequired,
            message: "Created date must be specified.");

        /// <summary>Error for when the creation timestamp is too far in the future.</summary>
        public static Error CreatedAtInFuture => Error.UnprocessableEntity(
            code: Codes.CreatedAtInFuture,
            message:
                $"Created date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the entity is already initialized.</summary>
        public static Error AlreadyInitialized => Error.UnprocessableEntity(
            code: Codes.AlreadyInitialized,
            message:
                "Creation audit metadata is already set; it cannot be initialized twice.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error CreatedByTooLong => Error.UnprocessableEntity(
            code: Codes.CreatedByTooLong,
            message:
                $"Created by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error CreatedByTooShort => Error.UnprocessableEntity(
            code: Codes.CreatedByTooShort,
            message:
                $"Created by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error CreatedByRequired => Error.UnprocessableEntity(
            code: Codes.CreatedByRequired,
            message: "Created by is required.");

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
        public const string EntityRequired = "Creatable.Entity.Required";
        public const string CreatedAtRequired = "Creatable.CreatedAt.Required";
        public const string CreatedAtInFuture = "Creatable.CreatedAt.InFuture";
        public const string AlreadyInitialized = "Creatable.CreatedAt.AlreadyInitialized";
        public const string CreatedByTooLong = "Creatable.CreatedBy.TooLong";
        public const string CreatedByTooShort = "Creatable.CreatedBy.TooShort";
        public const string CreatedByRequired = "Creatable.CreatedBy.Required";
        public const string ActorInvalid = "Creatable.Actor.Invalid";
    }
}