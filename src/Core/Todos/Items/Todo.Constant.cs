namespace Core.Todos.Items;

/// <summary>Constants for <see cref="ITodo"/>.</summary>
public static class TodoConstant
{
    /// <summary>Constraint values for <see cref="ITodo"/>.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for the title.</summary>
        public static class Title
        {
            /// <summary>Minimum length for a title.</summary>
            public const int MinLength = 1;

            /// <summary>Maximum length for a title.</summary>
            public const int MaxLength = 200;
        }

        /// <summary>Length constraints for the description.</summary>
        public static class Description
        {
            /// <summary>Maximum length for a description.</summary>
            public const int MaxLength = 4000;
        }

        /// <summary>Collection constraints for the category references.</summary>
        public static class Collection
        {
            /// <summary>Maximum number of categories a todo may reference.</summary>
            public const int MaxCategories = 20;
        }
    }

    /// <summary>Default values for <see cref="ITodo"/>.</summary>
    public static class Defaults
    {
        /// <summary>The status a new todo starts in.</summary>
        public const TodoStatus Status = TodoStatus.Pending;

        /// <summary>The priority a new todo starts with.</summary>
        public const TodoPriority Priority = TodoPriority.Medium;
    }
}