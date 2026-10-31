namespace Core.Modules.Todos.Reminders;

/// <summary>Constants for <see cref="TodoReminder"/>.</summary>
public static class TodoReminderConstant
{
    public static class Defaults
    {
        public const ReminderStatus Status = ReminderStatus.Scheduled;
        public const int SnoozeCount = 0;
    }

    public static class Constraints
    {
        public const int MaxSnoozeCount = 100;
    }
}
