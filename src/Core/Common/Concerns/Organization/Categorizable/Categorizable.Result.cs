namespace Core.Common.Concerns.Organization.Categorizable;

/// <summary>Result constants for <see cref="ICategorizable{TCategory}"/>.</summary>
public static class CategorizableResult
{
    /// <summary>Failure error codes for categorizable operations.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error for when the entity is required but null.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Categorizable.Entity.Required",
            message: "Categorizable entity is required.");

        /// <summary>Error for when the category is not specified.</summary>
        public static Error CategoryRequired => Error.UnprocessableEntity(
            code: "Categorizable.Category.Required",
            message: "Category must be specified.");

        /// <summary>Error for when the category identifier is empty.</summary>
        public static Error CategoryEmpty => Error.UnprocessableEntity(
            code: "Categorizable.Category.Empty",
            message: "Category identifier must not be empty.");

        /// <summary>Error for when the category name exceeds the maximum length.</summary>
        public static Error CategoryTooLong => Error.UnprocessableEntity(
            code: "Categorizable.Category.TooLong",
            message:
                $"Category name cannot exceed {CategorizableConstant.Constraints.Category.MaxNameLength} characters.");

        /// <summary>Error for when the entity already has the maximum number of categories.</summary>
        public static Error CategoryLimitExceeded => Error.UnprocessableEntity(
            code: "Categorizable.Category.LimitExceeded",
            message:
                $"Entity cannot have more than {CategorizableConstant.Constraints.Category.MaxCount} categories.");

        /// <summary>Error for when the category is already assigned.</summary>
        public static Error CategoryDuplicate => Error.UnprocessableEntity(
            code: "Categorizable.Category.Duplicate",
            message: "Category is already assigned.");

        /// <summary>Error for when the category is not assigned.</summary>
        public static Error CategoryNotFound => Error.UnprocessableEntity(
            code: "Categorizable.Category.NotFound",
            message: "Category is not assigned.");

        #endregion
    }
}