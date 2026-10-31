namespace Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Modifiable;

/// <summary>Failure error codes for the Modifiable concern.</summary>
public static class ModifiableOutcome
{
    /// <summary>Failure error codes for the <see cref="Modifiable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but not provided.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Modifiable entity is required.");

        /// <summary>Error for when the entity has no creation audit metadata.</summary>
        public static Error NotInitialized => Error.UnprocessableEntity(
            code: Codes.NotInitialized,
            message:
                "Entity has no creation audit metadata; initialize it before marking it modified.");

        /// <summary>Error for when the modification timestamp is not specified.</summary>
        public static Error ModifiedAtRequired => Error.UnprocessableEntity(
            code: Codes.ModifiedAtRequired,
            message: "Modified date must be specified.");

        /// <summary>Error for when the modification timestamp is too far in the future.</summary>
        public static Error ModifiedAtInFuture => Error.UnprocessableEntity(
            code: Codes.ModifiedAtInFuture,
            message:
                $"Modified date cannot be more than {LifecycleConstant.Constraints.Timestamp.MaxFutureSkewSeconds} seconds in the future.");

        /// <summary>Error for when the modification timestamp is before the creation timestamp.</summary>
        public static Error ModifiedAtBeforeCreatedAt => Error.UnprocessableEntity(
            code: Codes.ModifiedAtBeforeCreatedAt,
            message: "Modified date cannot be earlier than created date.");

        /// <summary>Error for when the actor exceeds the maximum length.</summary>
        public static Error ModifiedByTooLong => Error.UnprocessableEntity(
            code: Codes.ModifiedByTooLong,
            message:
                $"Modified by cannot exceed {LifecycleConstant.Constraints.Actor.MaxLength} characters.");

        /// <summary>Error for when the actor is shorter than the minimum length.</summary>
        public static Error ModifiedByTooShort => Error.UnprocessableEntity(
            code: Codes.ModifiedByTooShort,
            message:
                $"Modified by must be at least {LifecycleConstant.Constraints.Actor.MinLength} characters.");

        /// <summary>Error for when the actor is required but not provided.</summary>
        public static Error ModifiedByRequired => Error.UnprocessableEntity(
            code: Codes.ModifiedByRequired,
            message: "Modified by is required when the entity has been modified.");

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
        public const string EntityRequired = "Modifiable.Entity.Required";
        public const string NotInitialized = "Modifiable.NotInitialized";
        public const string ModifiedAtRequired = "Modifiable.ModifiedAt.Required";
        public const string ModifiedAtInFuture = "Modifiable.ModifiedAt.InFuture";
        public const string ModifiedAtBeforeCreatedAt = "Modifiable.ModifiedAt.InvalidRange";
        public const string ModifiedByTooLong = "Modifiable.ModifiedBy.TooLong";
        public const string ModifiedByTooShort = "Modifiable.ModifiedBy.TooShort";
        public const string ModifiedByRequired = "Modifiable.ModifiedBy.Required";
        public const string ActorInvalid = "Modifiable.Actor.Invalid";
    }
}