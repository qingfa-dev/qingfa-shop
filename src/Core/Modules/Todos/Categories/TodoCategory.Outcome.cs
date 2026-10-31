namespace Core.Modules.Todos.Categories;

/// <summary>Result constants for <see cref="ITodoCategory"/>.</summary>
public static class TodoCategoryOutcome
{
    /// <summary>Failure error constants for <see cref="ITodoCategory"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the category is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Todo category must be specified.");

        /// <summary>Error returned when the name is missing.</summary>
        public static Error NameRequired => Error.BadRequest(
            code: Codes.NameRequired,
            message: "Todo category name must be specified.");

        /// <summary>Error returned when the name exceeds its maximum length.</summary>
        public static Error NameTooLong => Error.BadRequest(
            code: Codes.NameTooLong,
            message:
                $"Todo category name cannot exceed {TodoCategoryConstant.Constraints.Name.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: Codes.DescriptionTooLong,
            message:
                $"Todo category description cannot exceed {TodoCategoryConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when a deleted category is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: Codes.DeletedNotMutable,
            message: "A deleted todo category cannot be modified.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "TodoCategory.Entity.Required";
        public const string NameRequired = "TodoCategory.Name.Required";
        public const string NameTooLong = "TodoCategory.Name.TooLong";
        public const string DescriptionTooLong = "TodoCategory.Description.TooLong";
        public const string DeletedNotMutable = "TodoCategory.Deleted.NotMutable";
    }
}