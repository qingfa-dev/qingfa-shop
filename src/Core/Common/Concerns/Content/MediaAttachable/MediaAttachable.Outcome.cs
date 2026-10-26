namespace Core.Common.Concerns.Content.MediaAttachable;

/// <summary>Result constants for the <see cref="MediaAttachable"/> concern.</summary>
public static class MediaAttachableOutcome
{
    /// <summary>Failure error constants for the <see cref="MediaAttachable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the media-attachable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "MediaAttachable.Entity.Required",
            message: "Media-attachable entity is required.");

        /// <summary>Error when the media is not specified.</summary>
        public static Error MediaRequired => Error.UnprocessableEntity(
            code: "MediaAttachable.Media.Required",
            message: "Media must be specified.");

        /// <summary>Error when the media is already attached.</summary>
        public static Error MediaDuplicate => Error.UnprocessableEntity(
            code: "MediaAttachable.Media.Duplicate",
            message: "Media is already attached.");

        /// <summary>Error when the media is not attached.</summary>
        public static Error MediaNotFound => Error.UnprocessableEntity(
            code: "MediaAttachable.Media.NotFound",
            message: "Media is not attached.");

        /// <summary>Error when the entity already has the maximum number of media items.</summary>
        public static Error MediaTooMany => Error.UnprocessableEntity(
            code: "MediaAttachable.Media.TooMany",
            message:
                $"Cannot attach more than {MediaAttachableConstant.Constraints.Collection.MaxAttachments} media items.");

        /// <summary>Error when the media URL is not specified.</summary>
        public static Error UrlRequired => Error.UnprocessableEntity(
            code: "MediaAttachable.Url.Required",
            message: "Media URL must be specified.");

        /// <summary>Error when the media URL exceeds the maximum length.</summary>
        public static Error UrlTooLong => Error.UnprocessableEntity(
            code: "MediaAttachable.Url.TooLong",
            message:
                $"Media URL cannot exceed {MediaAttachableConstant.Constraints.Url.MaxLength} characters.");

        /// <summary>Error when the media URL is not a valid http/https URL.</summary>
        public static Error UrlInvalid => Error.UnprocessableEntity(
            code: "MediaAttachable.Url.Invalid",
            message: "Media URL must be a valid http(s) URL.");

        /// <summary>Error when the media alt text exceeds the maximum length.</summary>
        public static Error AltTextTooLong => Error.UnprocessableEntity(
            code: "MediaAttachable.AltText.TooLong",
            message:
                $"Media alt text cannot exceed {MediaAttachableConstant.Constraints.AltText.MaxLength} characters.");

        /// <summary>Error when the media type is not specified.</summary>
        public static Error MediaTypeRequired => Error.UnprocessableEntity(
            code: "MediaAttachable.MediaType.Required",
            message: "Media type must be specified.");

        /// <summary>Error when the media type is not in the allowed list.</summary>
        public static Error MediaTypeInvalid => Error.UnprocessableEntity(
            code: "MediaAttachable.MediaType.Invalid",
            message:
                $"Media type must be one of: {string.Join(", ", MediaAttachableConstant.Defaults.MediaType.Allowed)}.");

        #endregion
    }
}