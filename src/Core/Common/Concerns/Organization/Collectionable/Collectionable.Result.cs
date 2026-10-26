namespace Core.Common.Concerns.Organization.Collectionable;

/// <summary>Result constants for <see cref="ICollectionable{TCollection}"/>.</summary>
public static class CollectionableResult
{
    /// <summary>Failure error codes for collectionable operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Collectionable.Entity.Required",
            message: "Collectionable entity is required.");

        /// <summary>Error for when the collection is not specified.</summary>
        public static Error CollectionRequired => Error.UnprocessableEntity(
            code: "Collectionable.Collection.Required",
            message: "Collection must be specified.");

        /// <summary>Error for when the collection identifier is empty.</summary>
        public static Error CollectionEmpty => Error.UnprocessableEntity(
            code: "Collectionable.Collection.Empty",
            message: "Collection identifier must not be empty.");

        /// <summary>Error for when the collection name exceeds the maximum length.</summary>
        public static Error CollectionTooLong => Error.UnprocessableEntity(
            code: "Collectionable.Collection.TooLong",
            message:
                $"Collection name cannot exceed {CollectionableConstant.Constraints.Collection.MaxNameLength} characters.");

        /// <summary>Error for when the entity already has the maximum number of collections.</summary>
        public static Error CollectionLimitExceeded => Error.UnprocessableEntity(
            code: "Collectionable.Collection.LimitExceeded",
            message:
                $"Entity cannot have more than {CollectionableConstant.Constraints.Collection.MaxCount} collections.");

        /// <summary>Error for when the collection is already assigned.</summary>
        public static Error CollectionDuplicate => Error.UnprocessableEntity(
            code: "Collectionable.Collection.Duplicate",
            message: "Collection is already assigned.");

        /// <summary>Error for when the collection is not assigned.</summary>
        public static Error CollectionNotFound => Error.UnprocessableEntity(
            code: "Collectionable.Collection.NotFound",
            message: "Collection is not assigned.");

        #endregion
    }
}