namespace Core.Todos.Items;

/// <summary>Result constants for <see cref="ITodo"/>.</summary>
public static class TodoOutcome
{
    /// <summary>Failure error constants for <see cref="ITodo"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the todo is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Todo.Entity.Required",
            message: "Todo must be specified.");

        /// <summary>Error returned when the title is missing.</summary>
        public static Error TitleRequired => Error.BadRequest(
            code: "Todo.Title.Required",
            message: "Todo title must be specified.");

        /// <summary>Error returned when the title exceeds its maximum length.</summary>
        public static Error TitleTooLong => Error.BadRequest(
            code: "Todo.Title.TooLong",
            message:
                $"Todo title cannot exceed {TodoConstant.Constraints.Title.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: "Todo.Description.TooLong",
            message:
                $"Todo description cannot exceed {TodoConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when the status is not a defined value.</summary>
        public static Error StatusInvalid => Error.BadRequest(
            code: "Todo.Status.Invalid",
            message: "Todo status is not a defined value.");

        /// <summary>Error returned when the priority is not a defined value.</summary>
        public static Error PriorityInvalid => Error.BadRequest(
            code: "Todo.Priority.Invalid",
            message: "Todo priority is not a defined value.");

        /// <summary>Error returned when the due date precedes the creation date.</summary>
        public static Error DueAtBeforeCreated => Error.BadRequest(
            code: "Todo.DueAt.BeforeCreated",
            message: "Todo due date cannot precede the created date.");

        /// <summary>Error returned when the creation instant is missing.</summary>
        public static Error CreatedAtRequired => Error.BadRequest(
            code: "Todo.CreatedAt.Required",
            message: "Todo creation instant must be specified.");

        /// <summary>Error returned when a category reference is missing.</summary>
        public static Error CategoryRequired => Error.BadRequest(
            code: "Todo.Category.Required",
            message: "Todo category identifier must be specified.");

        /// <summary>Error returned when a category is referenced twice.</summary>
        public static Error CategoryDuplicate => Error.Conflict(
            code: "Todo.Category.Duplicate",
            message: "Todo already references this category.");

        /// <summary>Error returned when a category is not referenced.</summary>
        public static Error CategoryNotFound => Error.UnprocessableEntity(
            code: "Todo.Category.NotFound",
            message: "Todo does not reference this category.");

        /// <summary>Error returned when the category limit is exceeded.</summary>
        public static Error CategoriesTooMany => Error.BadRequest(
            code: "Todo.Categories.TooMany",
            message:
                $"Todo cannot reference more than {TodoConstant.Constraints.Collection.MaxCategories} categories.");

        /// <summary>Error returned when the list reference is missing.</summary>
        public static Error ListIdRequired => Error.BadRequest(
            code: "Todo.ListId.Required",
            message: "Todo list identifier must be specified.");

        /// <summary>Error returned when the requested status move is not legal.</summary>
        public static Error StatusTransitionInvalid => Error.Conflict(
            code: "Todo.Status.TransitionInvalid",
            message: "Todo status cannot move to the requested state.");

        /// <summary>Error returned when a deleted todo is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: "Todo.Deleted.NotMutable",
            message: "A deleted todo cannot be modified.");

        /// <summary>Error returned when a hard-deleted stream is restored or mutated.</summary>
        public static Error HardDeletedNotFound => Error.NotFound(
            code: "Todo.Deleted.Hard",
            message: "The todo was permanently deleted.");
    }
}