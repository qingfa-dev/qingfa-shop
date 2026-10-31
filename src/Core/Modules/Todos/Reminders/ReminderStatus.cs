namespace Core.Modules.Todos.Reminders;

/// <summary>Lifecycle of a todo reminder.</summary>
public enum ReminderStatus
{
    Scheduled = 1,
    Snoozed = 2,
    Dismissed = 3,
}
