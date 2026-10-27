namespace Core.Todos.Lists;

/// <summary>Constants for <see cref="ITodoList"/>.</summary>
public static class TodoListConstant
{
    /// <summary>Constraint values for <see cref="ITodoList"/>.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for the name.</summary>
        public static class Name
        {
            /// <summary>Minimum length for a name.</summary>
            public const int MinLength = 1;

            /// <summary>Maximum length for a name.</summary>
            public const int MaxLength = 128;
        }

        /// <summary>Length constraints for the description.</summary>
        public static class Description
        {
            /// <summary>Maximum length for a description.</summary>
            public const int MaxLength = 1000;
        }
    }
}