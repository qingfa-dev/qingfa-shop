namespace Core.BuildingBlocks.Modeling.Concerns.Organization.Collectionable;

/// <summary>Constants for the <see cref="Collectionable"/> concern.</summary>
public static class CollectionableConstant
{
    #region Constraints

    /// <summary>Constraint values for the <see cref="Collectionable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Collection value constraints.</summary>
        public static class Collection
        {
            /// <summary>Minimum length for a string collection name.</summary>
            public const int MinNameLength = 1;

            /// <summary>Maximum length for a string collection name.</summary>
            public const int MaxNameLength = 128;

            /// <summary>Maximum number of collections per entity.</summary>
            public const int MaxCount = 32;
        }
    }

    #endregion

    #region Defaults

    /// <summary>Default option values for the <see cref="Collectionable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Default value for trimming string collections before validation.</summary>
        public const bool TrimValues = true;
    }

    #endregion

    #region Patterns

    /// <summary>Regex patterns for the <see cref="Collectionable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for collection name validation (no leading/trailing whitespace).</summary>
        public const string Name = @"^\S(?:.*\S)?$";

        /// <summary>Pattern for slug-style collection handles (e.g. <c>"summer-2026"</c>).</summary>
        public const string Slug = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
    }

    #endregion
}