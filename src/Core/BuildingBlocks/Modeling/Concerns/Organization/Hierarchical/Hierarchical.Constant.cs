namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Hierarchical;

/// <summary>Constants for the <see cref="Hierarchical"/> concern.</summary>
/// <remarks>
/// Cycle detection note: the building block only sees a single <c>ParentId</c>
/// assignment. Callers that own the tree must pass the entity id and its
/// ancestor chain to the <c>ValidateParent</c> overloads so self-parent and
/// cycle assignments can be rejected before mutation.
/// </remarks>
public static class HierarchicalConstant
{
    #region Constraints

    /// <summary>Constraint values for the <see cref="Hierarchical"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Parent assignment constraints.</summary>
        public static class Parent
        {
            /// <summary>Maximum allowed hierarchy depth (root = 0).</summary>
            public const int MaxDepth = 32;

            /// <summary>Maximum allowed materialized path length in characters.</summary>
            public const int MaxPathLength = 1024;
        }

        /// <summary>Level constraints.</summary>
        public static class Level
        {
            /// <summary>Minimum allowed level (root).</summary>
            public const int Min = 0;

            /// <summary>Maximum allowed level.</summary>
            public const int Max = 32;
        }
    }

    #endregion

    #region Defaults

    /// <summary>Default option values for the <see cref="Hierarchical"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Default path separator for materialized paths.</summary>
        public const char PathSeparator = '/';

        /// <summary>Default level for root entities.</summary>
        public const int RootLevel = 0;
    }

    #endregion

    #region Patterns

    /// <summary>Regex patterns for the <see cref="Hierarchical"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for materialized path validation (segments separated by '/').</summary>
        public const string Path = @"^[^/]+(?:/[^/]+)*$";
    }

    #endregion
}