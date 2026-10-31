namespace Core.BuildingBlocks.Modeling.Concerns.Content.Taggable;

/// <summary>Constants for the <see cref="Taggable"/> concern.</summary>
public static class TaggableConstant
{
    /// <summary>Constraint values for the <see cref="Taggable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a single tag.</summary>
        public static class Tag
        {
            /// <summary>Minimum length for a tag.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for a tag.</summary>
            public const int MaxLength = 128;
        }

        /// <summary>Count constraints for the tag collection.</summary>
        public static class Collection
        {
            /// <summary>Minimum number of tags retained on an entity.</summary>
            public const int MinTags = 0;
            /// <summary>Maximum number of tags allowed on an entity.</summary>
            public const int MaxTags = 50;
        }
    }

    /// <summary>Default values for the <see cref="Taggable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Normalization defaults applied to tags before validation.</summary>
        public static class Normalization
        {
            /// <summary>Whether surrounding whitespace is trimmed.</summary>
            public const bool TrimWhitespace = true;
            /// <summary>Whether tags are lowercased.</summary>
            public const bool ToLowercase = true;
        }
    }

    /// <summary>Regex patterns for the <see cref="Taggable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for tag validation (no leading/trailing whitespace).</summary>
        public const string Tag = @"^\S(?:.*\S)?$";
    }
}