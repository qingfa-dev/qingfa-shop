namespace Core.Todos.Reminders;

/// <summary>Stable failures for reminder scheduling and transitions.</summary>
public static class TodoReminderOutcome
{
    public static class Failure
    {
        public static Error EntityRequired => Error.UnprocessableEntity(
            Codes.EntityRequired, "Todo reminder must be specified.");

        public static Error IdRequired => Error.BadRequest(
            Codes.IdRequired, "Reminder id must be specified.");

        public static Error TodoIdRequired => Error.BadRequest(
            Codes.TodoIdRequired, "Todo id must be specified.");

        public static Error TenantIdRequired => Error.BadRequest(
            Codes.TenantIdRequired, "Tenant id must be specified.");

        public static Error RemindAtRequired => Error.BadRequest(
            Codes.RemindAtRequired, "Reminder time must be specified.");

        public static Error RemindAtInPast => Error.BadRequest(
            Codes.RemindAtInPast, "Reminder time must be in the future.");

        public static Error NotFound => Error.NotFound(
            Codes.NotFound, "The reminder does not exist for the current tenant.");

        public static Error AlreadyDismissed => Error.Conflict(
            Codes.AlreadyDismissed, "A dismissed reminder cannot be changed.");

        public static Error SnoozeLimit => Error.Conflict(
            Codes.SnoozeLimit,
            $"A reminder cannot be snoozed more than {TodoReminderConstant.Constraints.MaxSnoozeCount} times.");

        public static Error SnapshotInvalid => Error.InternalServerError(
            Codes.SnapshotInvalid, "The reminder snapshot is invalid.");
    }

    public static class Codes
    {
        public const string EntityRequired = "TodoReminder.Entity.Required";
        public const string IdRequired = "TodoReminder.Id.Required";
        public const string TodoIdRequired = "TodoReminder.TodoId.Required";
        public const string TenantIdRequired = "TodoReminder.TenantId.Required";
        public const string RemindAtRequired = "TodoReminder.RemindAt.Required";
        public const string RemindAtInPast = "TodoReminder.RemindAt.InPast";
        public const string NotFound = "TodoReminder.Entity.NotFound";
        public const string AlreadyDismissed = "TodoReminder.Status.AlreadyDismissed";
        public const string SnoozeLimit = "TodoReminder.Snooze.Limit";
        public const string SnapshotInvalid = "TodoReminder.Snapshot.Invalid";
    }
}
