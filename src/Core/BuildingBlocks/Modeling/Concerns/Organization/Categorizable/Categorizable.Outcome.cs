namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Categorizable;

/// <summary>Result constants for <see cref="ICategorizable{TCategory}"/>.</summary>
public static class CategorizableOutcome
{
    /// <summary>Failure error codes for categorizable operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Categorizable entity is required.");

        /// <summary>Error for when the category is not specified.</summary>
        public static Error CategoryRequired => Error.UnprocessableEntity(
            code: Codes.CategoryRequired,
            message: "Category must be specified.");

        /// <summary>Error for when the category identifier is empty.</summary>
        public static Error CategoryEmpty => Error.UnprocessableEntity(
            code: Codes.CategoryEmpty,
            message: "Category identifier must not be empty.");

        /// <summary>Error for when the category name exceeds the maximum length.</summary>
        public static Error CategoryTooLong => Error.UnprocessableEntity(
            code: Codes.CategoryTooLong,
            message:
                $"Category name cannot exceed {CategorizableConstant.Constraints.Category.MaxNameLength} characters.");

        /// <summary>Error for when the entity already has the maximum number of categories.</summary>
        public static Error CategoryLimitExceeded => Error.UnprocessableEntity(
            code: Codes.CategoryLimitExceeded,
            message:
                $"Entity cannot have more than {CategorizableConstant.Constraints.Category.MaxCount} categories.");

        /// <summary>Error for when the category is already assigned.</summary>
        public static Error CategoryDuplicate => Error.UnprocessableEntity(
            code: Codes.CategoryDuplicate,
            message: "Category is already assigned.");

        /// <summary>Error for when the category is not assigned.</summary>
        public static Error CategoryNotFound => Error.UnprocessableEntity(
            code: Codes.CategoryNotFound,
            message: "Category is not assigned.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Categorizable.Entity.Required";
        public const string CategoryRequired = "Categorizable.Category.Required";
        public const string CategoryEmpty = "Categorizable.Category.Empty";
        public const string CategoryTooLong = "Categorizable.Category.TooLong";
        public const string CategoryLimitExceeded = "Categorizable.Category.LimitExceeded";
        public const string CategoryDuplicate = "Categorizable.Category.Duplicate";
        public const string CategoryNotFound = "Categorizable.Category.NotFound";
    }
}