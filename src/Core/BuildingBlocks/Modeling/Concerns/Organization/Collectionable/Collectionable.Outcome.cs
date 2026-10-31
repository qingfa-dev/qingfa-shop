namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Collectionable;

/// <summary>Result constants for <see cref="ICollectionable{TCollection}"/>.</summary>
public static class CollectionableOutcome
{
    /// <summary>Failure error codes for collectionable operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Collectionable entity is required.");

        /// <summary>Error for when the collection is not specified.</summary>
        public static Error CollectionRequired => Error.UnprocessableEntity(
            code: Codes.CollectionRequired,
            message: "Collection must be specified.");

        /// <summary>Error for when the collection identifier is empty.</summary>
        public static Error CollectionEmpty => Error.UnprocessableEntity(
            code: Codes.CollectionEmpty,
            message: "Collection identifier must not be empty.");

        /// <summary>Error for when the collection name exceeds the maximum length.</summary>
        public static Error CollectionTooLong => Error.UnprocessableEntity(
            code: Codes.CollectionTooLong,
            message:
                $"Collection name cannot exceed {CollectionableConstant.Constraints.Collection.MaxNameLength} characters.");

        /// <summary>Error for when the entity already has the maximum number of collections.</summary>
        public static Error CollectionLimitExceeded => Error.UnprocessableEntity(
            code: Codes.CollectionLimitExceeded,
            message:
                $"Entity cannot have more than {CollectionableConstant.Constraints.Collection.MaxCount} collections.");

        /// <summary>Error for when the collection is already assigned.</summary>
        public static Error CollectionDuplicate => Error.UnprocessableEntity(
            code: Codes.CollectionDuplicate,
            message: "Collection is already assigned.");

        /// <summary>Error for when the collection is not assigned.</summary>
        public static Error CollectionNotFound => Error.UnprocessableEntity(
            code: Codes.CollectionNotFound,
            message: "Collection is not assigned.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Collectionable.Entity.Required";
        public const string CollectionRequired = "Collectionable.Collection.Required";
        public const string CollectionEmpty = "Collectionable.Collection.Empty";
        public const string CollectionTooLong = "Collectionable.Collection.TooLong";
        public const string CollectionLimitExceeded = "Collectionable.Collection.LimitExceeded";
        public const string CollectionDuplicate = "Collectionable.Collection.Duplicate";
        public const string CollectionNotFound = "Collectionable.Collection.NotFound";
    }
}