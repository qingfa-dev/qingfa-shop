namespace Core.BuildingBlocks.Modeling.Concerns.Content.MediaAttachable;

/// <summary>Constants for the <see cref="MediaAttachable"/> concern.</summary>
public static class MediaAttachableConstant
{
    /// <summary>Constraint values for the <see cref="MediaAttachable"/> concern.</summary>
    public static class Constraints
    {
        /// <summary>Length constraints for a media URL.</summary>
        public static class Url
        {
            /// <summary>Maximum length for a media URL.</summary>
            public const int MaxLength = 2048;
        }

        /// <summary>Length constraints for media alt text.</summary>
        public static class AltText
        {
            /// <summary>Maximum length for media alt text.</summary>
            public const int MaxLength = 500;
        }

        /// <summary>Count constraints for the attached-media collection.</summary>
        public static class Collection
        {
            /// <summary>Minimum number of media items retained on an entity.</summary>
            public const int MinAttachments = 0;
            /// <summary>Maximum number of media items allowed on an entity.</summary>
            public const int MaxAttachments = 20;
        }
    }

    /// <summary>Default values for the <see cref="MediaAttachable"/> concern.</summary>
    public static class Defaults
    {
        /// <summary>Defaults for the media type discriminator.</summary>
        public static class MediaType
        {
            /// <summary>Default media type when none is specified.</summary>
            public const string Default = "image";

            /// <summary>Allowed media type values.</summary>
            public static readonly IReadOnlyList<string> Allowed =
            [
                "image",
                "video",
                "audio",
                "document",
            ];
        }
    }

    /// <summary>Regex patterns for the <see cref="MediaAttachable"/> concern.</summary>
    public static class Patterns
    {
        /// <summary>Pattern for media URL validation (http/https).</summary>
        public const string Url = @"^https?://[^\s/$.?#].[^\s]*$";
    }
}