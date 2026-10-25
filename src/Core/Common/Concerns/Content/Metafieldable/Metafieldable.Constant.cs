namespace Core.Common.Concerns.Content.Metafieldable;

/// <summary>Constants for the <see cref="Metafieldable"/> concern.</summary>
public static class MetafieldableConstant
{
    /// <summary>Constraint values for the <see cref="Metafieldable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a metafield namespace.</summary>
        public static class Namespace
        {
            /// <summary>Minimum length for a namespace.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for a namespace.</summary>
            public const int MaxLength = 64;
        }

        /// <summary>Length constraints for a metafield key.</summary>
        public static class Key
        {
            /// <summary>Minimum length for a key.</summary>
            public const int MinLength = 1;
            /// <summary>Maximum length for a key.</summary>
            public const int MaxLength = 64;
        }

        /// <summary>Length constraints for a metafield value.</summary>
        public static class Value
        {
            /// <summary>Maximum length for a metafield value.</summary>
            public const int MaxLength = 8000;
        }

        /// <summary>Count constraints for the metafield collection.</summary>
        public static class Collection
        {
            /// <summary>Maximum number of metafields allowed on an entity.</summary>
            public const int MaxMetafields = 100;
        }
    }

    /// <summary>Default values for the <see cref="Metafieldable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Defaults for the metafield type discriminator.</summary>
        public static class Type
        {
            /// <summary>Default metafield type when none is specified.</summary>
            public const MetafieldType Default = MetafieldType.ShortText;
        }
    }

    /// <summary>Regex patterns for the <see cref="Metafieldable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for snake_case validation.</summary>
        public const string SnakeCase = @"^[a-z0-9_]+$";
        /// <summary>Pattern for kebab-case validation.</summary>
        public const string KebabCase = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
    }
}