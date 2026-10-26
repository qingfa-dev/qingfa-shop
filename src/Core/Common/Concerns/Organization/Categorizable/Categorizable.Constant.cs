namespace Core.Common.Concerns.Organization.Categorizable;

/// <summary>Constants for the <see cref="Categorizable"/> concern.</summary>
public static class CategorizableConstant
{
    #region Constraints

    /// <summary>Constraint values for the <see cref="Categorizable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Category value constraints.</summary>
        public static class Category
        {
            /// <summary>Minimum length for a string category name.</summary>
            public const int MinNameLength = 1;

            /// <summary>Maximum length for a string category name.</summary>
            public const int MaxNameLength = 128;

            /// <summary>Maximum number of categories per entity.</summary>
            public const int MaxCount = 32;
        }
    }

    #endregion

    #region Defaults

    /// <summary>Default option values for the <see cref="Categorizable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Default value for trimming string categories before validation.</summary>
        public const bool TrimValues = true;
    }

    #endregion

    #region Patterns

    /// <summary>Regex patterns for the <see cref="Categorizable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for category name validation (no leading/trailing whitespace).</summary>
        public const string Name = @"^\S(?:.*\S)?$";
    }

    #endregion
}