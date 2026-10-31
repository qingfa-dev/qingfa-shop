namespace Core.Modules.Todos.Items;

/// <summary>Result constants for <see cref="ITodo"/>.</summary>
public static class TodoOutcome
{
    /// <summary>Failure error constants for <see cref="ITodo"/>.</summary>
    public static class Failure
    {
        /// <summary>Error returned when the todo identifier is missing.</summary>
        public static Error IdRequired => Error.BadRequest(
            code: Codes.IdRequired,
            message: "Todo identifier must be specified.");

        /// <summary>Error returned when the tenant is missing.</summary>
        public static Error TenantIdRequired => Error.BadRequest(
            code: Codes.TenantIdRequired,
            message: "Tenant identifier must be specified.");

        /// <summary>Error returned when an event payload violates an invariant.</summary>
        public static Error EventInvalid(string detail) => Error.InternalServerError(
            code: Codes.EventInvalid,
            message: $"The todo event is invalid: {detail}");

        /// <summary>Error returned when an event is outside the todo vocabulary.</summary>
        public static Error EventUnsupported(string eventName) => Error.InternalServerError(
            code: Codes.EventUnsupported,
            message: $"Event '{eventName}' is not supported by Todo.");

        /// <summary>Error returned when the todo is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Todo must be specified.");

        /// <summary>Error returned when the title is missing.</summary>
        public static Error TitleRequired => Error.BadRequest(
            code: Codes.TitleRequired,
            message: "Todo title must be specified.");

        /// <summary>Error returned when the title exceeds its maximum length.</summary>
        public static Error TitleTooLong => Error.BadRequest(
            code: Codes.TitleTooLong,
            message:
                $"Todo title cannot exceed {TodoConstant.Constraints.Title.MaxLength} characters.");

        /// <summary>Error returned when the description exceeds its maximum length.</summary>
        public static Error DescriptionTooLong => Error.BadRequest(
            code: Codes.DescriptionTooLong,
            message:
                $"Todo description cannot exceed {TodoConstant.Constraints.Description.MaxLength} characters.");

        /// <summary>Error returned when the status is not a defined value.</summary>
        public static Error StatusInvalid => Error.BadRequest(
            code: Codes.StatusInvalid,
            message: "Todo status is not a defined value.");

        /// <summary>Error returned when the priority is not a defined value.</summary>
        public static Error PriorityInvalid => Error.BadRequest(
            code: Codes.PriorityInvalid,
            message: "Todo priority is not a defined value.");

        /// <summary>Error returned when the due date precedes the creation date.</summary>
        public static Error DueAtBeforeCreated => Error.BadRequest(
            code: Codes.DueAtBeforeCreated,
            message: "Todo due date cannot precede the created date.");

        /// <summary>Error returned when the creation instant is missing.</summary>
        public static Error CreatedAtRequired => Error.BadRequest(
            code: Codes.CreatedAtRequired,
            message: "Todo creation instant must be specified.");

        /// <summary>Error returned when a category reference is missing.</summary>
        public static Error CategoryRequired => Error.BadRequest(
            code: Codes.CategoryRequired,
            message: "Todo category identifier must be specified.");

        /// <summary>Error returned when a category is referenced twice.</summary>
        public static Error CategoryDuplicate => Error.Conflict(
            code: Codes.CategoryDuplicate,
            message: "Todo already references this category.");

        /// <summary>Error returned when a category is not referenced.</summary>
        public static Error CategoryNotFound => Error.UnprocessableEntity(
            code: Codes.CategoryNotFound,
            message: "Todo does not reference this category.");

        /// <summary>Error returned when the category limit is exceeded.</summary>
        public static Error CategoriesTooMany => Error.BadRequest(
            code: Codes.CategoriesTooMany,
            message:
                $"Todo cannot reference more than {TodoConstant.Constraints.Collection.MaxCategories} categories.");

        /// <summary>Error returned when the list reference is missing.</summary>
        public static Error ListIdRequired => Error.BadRequest(
            code: Codes.ListIdRequired,
            message: "Todo list identifier must be specified.");

        /// <summary>Error returned when the requested status move is not legal.</summary>
        public static Error StatusTransitionInvalid => Error.Conflict(
            code: Codes.StatusTransitionInvalid,
            message: "Todo status cannot move to the requested state.");

        /// <summary>Error returned when a deleted todo is mutated.</summary>
        public static Error DeletedNotMutable => Error.Conflict(
            code: Codes.DeletedNotMutable,
            message: "A deleted todo cannot be modified.");

        /// <summary>Error returned when a hard-deleted stream is restored or mutated.</summary>
        public static Error HardDeletedNotFound => Error.NotFound(
            code: Codes.HardDeletedNotFound,
            message: "The todo was permanently deleted.");
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string IdRequired = "Todo.Id.Required";
        public const string TenantIdRequired = "Todo.TenantId.Required";
        public const string EventInvalid = "Todo.Event.Invalid";
        public const string EventUnsupported = "Todo.Event.Unsupported";
        public const string EntityRequired = "Todo.Entity.Required";
        public const string TitleRequired = "Todo.Title.Required";
        public const string TitleTooLong = "Todo.Title.TooLong";
        public const string DescriptionTooLong = "Todo.Description.TooLong";
        public const string StatusInvalid = "Todo.Status.Invalid";
        public const string PriorityInvalid = "Todo.Priority.Invalid";
        public const string DueAtBeforeCreated = "Todo.DueAt.BeforeCreated";
        public const string CreatedAtRequired = "Todo.CreatedAt.Required";
        public const string CategoryRequired = "Todo.Category.Required";
        public const string CategoryDuplicate = "Todo.Category.Duplicate";
        public const string CategoryNotFound = "Todo.Category.NotFound";
        public const string CategoriesTooMany = "Todo.Categories.TooMany";
        public const string ListIdRequired = "Todo.ListId.Required";
        public const string StatusTransitionInvalid = "Todo.Status.TransitionInvalid";
        public const string DeletedNotMutable = "Todo.Deleted.NotMutable";
        public const string HardDeletedNotFound = "Todo.Deleted.Hard";
    }
}