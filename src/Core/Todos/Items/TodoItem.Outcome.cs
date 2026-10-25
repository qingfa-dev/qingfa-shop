namespace Core.Todos.Items;

/// <summary>Result constants for <see cref="ITodoItem"/>.</summary>
public static class TodoItemOutcome
{
    /// <summary>Failure error constants for <see cref="ITodoItem"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the todo is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "TodoItem.Entity.Required",
            message: "Todo must be specified.");

        /// <summary>Error returned when the title is missing.</summary>
        public static Error TitleRequired => Error.BadRequest(
            code: "TodoItem.Title.Required",
            message: "Todo title must be specified.");

        /// <summary>Error returned when the title exceeds its maximum length.</summary>
        public static Error TitleTooLong => Error.BadRequest(
            code: "TodoItem.Title.TooLong",
            message:
                $"Todo title cannot exceed {TodoItemConstant.Constraints.Title.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: "TodoItem.Description.TooLong",
            message:
                $"Todo description cannot exceed {TodoItemConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when the status is not a defined value.</summary>
        public static Error StatusInvalid => Error.BadRequest(
            code: "TodoItem.Status.Invalid",
            message: "Todo status is not a defined value.");

        /// <summary>Error returned when the priority is not a defined value.</summary>
        public static Error PriorityInvalid => Error.BadRequest(
            code: "TodoItem.Priority.Invalid",
            message: "Todo priority is not a defined value.");

        /// <summary>Error returned when the due date precedes the creation date.</summary>
        public static Error DueAtBeforeCreated => Error.BadRequest(
            code: "TodoItem.DueAt.BeforeCreated",
            message: "Todo due date cannot precede the created date.");

        /// <summary>Error returned when a category reference is missing.</summary>
        public static Error CategoryRequired => Error.BadRequest(
            code: "TodoItem.Category.Required",
            message: "Todo category identifier must be specified.");

        /// <summary>Error returned when a category is referenced twice.</summary>
        public static Error CategoryDuplicate => Error.Conflict(
            code: "TodoItem.Category.Duplicate",
            message: "Todo already references this category.");

        /// <summary>Error returned when a category is not referenced.</summary>
        public static Error CategoryNotFound => Error.UnprocessableEntity(
            code: "TodoItem.Category.NotFound",
            message: "Todo does not reference this category.");

        /// <summary>Error returned when the category limit is exceeded.</summary>
        public static Error CategoriesTooMany => Error.BadRequest(
            code: "TodoItem.Categories.TooMany",
            message:
                $"Todo cannot reference more than {TodoItemConstant.Constraints.Collection.MaxCategories} categories.");

        /// <summary>Error returned when the list reference is missing.</summary>
        public static Error ListIdRequired => Error.BadRequest(
            code: "TodoItem.ListId.Required",
            message: "Todo list identifier must be specified.");

        /// <summary>Error returned when the requested status move is not legal.</summary>
        public static Error StatusTransitionInvalid => Error.Conflict(
            code: "TodoItem.Status.TransitionInvalid",
            message: "Todo status cannot move to the requested state.");

        /// <summary>Error returned when a deleted todo is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: "TodoItem.Deleted.NotMutable",
            message: "A deleted todo cannot be modified.");
    }
}