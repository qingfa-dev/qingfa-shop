namespace Application.Modules.Todos.Events;

/// <summary>Stable wire identities for Todo integration events.</summary>
public static class TodoIntegrationConstant
{
    /// <summary>Integration event names shared by publishers and consumers.</summary>
    public static class Events
    {
        /// <summary>Todo completed event name.</summary>
        public const string Completed = "todo.completed";

        /// <summary>Todo reminder changed event name.</summary>
        public const string ReminderChanged = "todo.reminder.changed";
    }
}
