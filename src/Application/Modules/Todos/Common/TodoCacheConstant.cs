namespace Application.Modules.Todos.Common;

/// <summary>Cache key prefixes for Todo read models.</summary>
public static class TodoCacheConstant
{
    /// <summary>Key prefixes used by Todo handlers.</summary>
    public static class Keys
    {
        /// <summary>Prefix for a single todo entry.</summary>
        public const string TodoPrefix = "todo:";

        /// <summary>Prefix for a reminder entry.</summary>
        public const string ReminderPrefix = "reminder:";
    }
}
