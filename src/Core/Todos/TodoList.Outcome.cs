namespace Core.Todos;

/// <summary>Result constants for <see cref="ITodoList"/>.</summary>
public static class TodoListOutcome
{
    /// <summary>Failure error constants for <see cref="ITodoList"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the list is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "TodoList.Entity.Required",
            message: "Todo list must be specified.");

        /// <summary>Error returned when the name is missing.</summary>
        public static Error NameRequired => Error.BadRequest(
            code: "TodoList.Name.Required",
            message: "Todo list name must be specified.");

        /// <summary>Error returned when the name exceeds its maximum length.</summary>
        public static Error NameTooLong => Error.BadRequest(
            code: "TodoList.Name.TooLong",
            message:
                $"Todo list name cannot exceed {TodoListConstant.Constraints.Name.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: "TodoList.Description.TooLong",
            message:
                $"Todo list description cannot exceed {TodoListConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when a deleted list is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: "TodoList.Deleted.NotMutable",
            message: "A deleted todo list cannot be modified.");
    }
}