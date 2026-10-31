namespace Core.Modules.Todos.Lists;

/// <summary>Result constants for <see cref="ITodoList"/>.</summary>
public static class TodoListOutcome
{
    /// <summary>Failure error constants for <see cref="ITodoList"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the list is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Todo list must be specified.");

        /// <summary>Error returned when the name is missing.</summary>
        public static Error NameRequired => Error.BadRequest(
            code: Codes.NameRequired,
            message: "Todo list name must be specified.");

        /// <summary>Error returned when the name exceeds its maximum length.</summary>
        public static Error NameTooLong => Error.BadRequest(
            code: Codes.NameTooLong,
            message:
                $"Todo list name cannot exceed {TodoListConstant.Constraints.Name.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: Codes.DescriptionTooLong,
            message:
                $"Todo list description cannot exceed {TodoListConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when a deleted list is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: Codes.DeletedNotMutable,
            message: "A deleted todo list cannot be modified.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "TodoList.Entity.Required";
        public const string NameRequired = "TodoList.Name.Required";
        public const string NameTooLong = "TodoList.Name.TooLong";
        public const string DescriptionTooLong = "TodoList.Description.TooLong";
        public const string DeletedNotMutable = "TodoList.Deleted.NotMutable";
    }
}