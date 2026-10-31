namespace Infrastructure.Modules.Todos.Persistence.Projections;

/// <summary>Stable projection names for the Todos module.</summary>
public static class TodoProjectionConstant
{
    /// <summary>Projection names recorded in checkpoints.</summary>
    public static class Names
    {
        /// <summary>Reminder projection name.</summary>
        public const string Reminders = "todo-reminders";
    }
}
