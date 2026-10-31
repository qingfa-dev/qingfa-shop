namespace Core.BuildingBlocks.Modeling.Concerns.Content.MediaAttachable;

/// <summary>Result constants for the <see cref="MediaAttachable"/> concern.</summary>
public static class MediaAttachableOutcome
{
    /// <summary>Failure error constants for the <see cref="MediaAttachable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the media-attachable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Media-attachable entity is required.");

        /// <summary>Error when the media is not specified.</summary>
        public static Error MediaRequired => Error.UnprocessableEntity(
            code: Codes.MediaRequired,
            message: "Media must be specified.");

        /// <summary>Error when the media is already attached.</summary>
        public static Error MediaDuplicate => Error.UnprocessableEntity(
            code: Codes.MediaDuplicate,
            message: "Media is already attached.");

        /// <summary>Error when the media is not attached.</summary>
        public static Error MediaNotFound => Error.UnprocessableEntity(
            code: Codes.MediaNotFound,
            message: "Media is not attached.");

        /// <summary>Error when the entity already has the maximum number of media items.</summary>
        public static Error MediaTooMany => Error.UnprocessableEntity(
            code: Codes.MediaTooMany,
            message:
                $"Cannot attach more than {MediaAttachableConstant.Constraints.Collection.MaxAttachments} media items.");

        /// <summary>Error when the media URL is not specified.</summary>
        public static Error UrlRequired => Error.UnprocessableEntity(
            code: Codes.UrlRequired,
            message: "Media URL must be specified.");

        /// <summary>Error when the media URL exceeds the maximum length.</summary>
        public static Error UrlTooLong => Error.UnprocessableEntity(
            code: Codes.UrlTooLong,
            message:
                $"Media URL cannot exceed {MediaAttachableConstant.Constraints.Url.MaxLength} characters.");

        /// <summary>Error when the media URL is not a valid http/https URL.</summary>
        public static Error UrlInvalid => Error.UnprocessableEntity(
            code: Codes.UrlInvalid,
            message: "Media URL must be a valid http(s) URL.");

        /// <summary>Error when the media alt text exceeds the maximum length.</summary>
        public static Error AltTextTooLong => Error.UnprocessableEntity(
            code: Codes.AltTextTooLong,
            message:
                $"Media alt text cannot exceed {MediaAttachableConstant.Constraints.AltText.MaxLength} characters.");

        /// <summary>Error when the media type is not specified.</summary>
        public static Error MediaTypeRequired => Error.UnprocessableEntity(
            code: Codes.MediaTypeRequired,
            message: "Media type must be specified.");

        /// <summary>Error when the media type is not in the allowed list.</summary>
        public static Error MediaTypeInvalid => Error.UnprocessableEntity(
            code: Codes.MediaTypeInvalid,
            message:
                $"Media type must be one of: {string.Join(", ", MediaAttachableConstant.Defaults.MediaType.Allowed)}.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "MediaAttachable.Entity.Required";
        public const string MediaRequired = "MediaAttachable.Media.Required";
        public const string MediaDuplicate = "MediaAttachable.Media.Duplicate";
        public const string MediaNotFound = "MediaAttachable.Media.NotFound";
        public const string MediaTooMany = "MediaAttachable.Media.TooMany";
        public const string UrlRequired = "MediaAttachable.Url.Required";
        public const string UrlTooLong = "MediaAttachable.Url.TooLong";
        public const string UrlInvalid = "MediaAttachable.Url.Invalid";
        public const string AltTextTooLong = "MediaAttachable.AltText.TooLong";
        public const string MediaTypeRequired = "MediaAttachable.MediaType.Required";
        public const string MediaTypeInvalid = "MediaAttachable.MediaType.Invalid";
    }
}