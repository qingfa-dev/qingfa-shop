namespace Core.Todos;

/// <summary>Result constants for <see cref="ITodoCategory"/>.</summary>
public static class TodoCategoryOutcome
{
    /// <summary>Failure error constants for <see cref="ITodoCategory"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the category is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "TodoCategory.Entity.Required",
            message: "Todo category must be specified.");

        /// <summary>Error returned when the name is missing.</summary>
        public static Error NameRequired => Error.BadRequest(
            code: "TodoCategory.Name.Required",
            message: "Todo category name must be specified.");

        /// <summary>Error returned when the name exceeds its maximum length.</summary>
        public static Error NameTooLong => Error.BadRequest(
            code: "TodoCategory.Name.TooLong",
            message:
                $"Todo category name cannot exceed {TodoCategoryConstant.Constraints.Name.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: "TodoCategory.Description.TooLong",
            message:
                $"Todo category description cannot exceed {TodoCategoryConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when a deleted category is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: "TodoCategory.Deleted.NotMutable",
            message: "A deleted todo category cannot be modified.");
    }
}