namespace Core.BuildingBlocks.Modeling.Concerns.Content.Taggable;

/// <summary>Result constants for the <see cref="Taggable"/> concern.</summary>
public static class TaggableOutcome
{
    /// <summary>Failure error constants for the <see cref="Taggable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the taggable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Taggable entity is required.");

        /// <summary>Error when the tag is not specified.</summary>
        public static Error TagRequired => Error.UnprocessableEntity(
            code: Codes.TagRequired,
            message: "Tag must be specified.");

        /// <summary>Error when the tag is shorter than the minimum length.</summary>
        public static Error TagTooShort => Error.UnprocessableEntity(
            code: Codes.TagTooShort,
            message:
                $"Tag must be at least {TaggableConstant.Constraints.Tag.MinLength} character(s).");

        /// <summary>Error when the tag exceeds the maximum length.</summary>
        public static Error TagTooLong => Error.UnprocessableEntity(
            code: Codes.TagTooLong,
            message:
                $"Tag cannot exceed {TaggableConstant.Constraints.Tag.MaxLength} characters.");

        /// <summary>Error when the tag has an invalid format.</summary>
        public static Error TagInvalid => Error.UnprocessableEntity(
            code: Codes.TagInvalid,
            message: "Tag must not have leading or trailing whitespace.");

        /// <summary>Error when the tag is already present.</summary>
        public static Error TagDuplicate => Error.UnprocessableEntity(
            code: Codes.TagDuplicate,
            message: "Tag is already present.");

        /// <summary>Error when the tag is not found.</summary>
        public static Error TagNotFound => Error.UnprocessableEntity(
            code: Codes.TagNotFound,
            message: "Tag is not present.");

        /// <summary>Error when the entity already has the maximum number of tags.</summary>
        public static Error TagsTooMany => Error.UnprocessableEntity(
            code: Codes.TagsTooMany,
            message:
                $"Cannot store more than {TaggableConstant.Constraints.Collection.MaxTags} tags.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Taggable.Entity.Required";
        public const string TagRequired = "Taggable.Tag.Required";
        public const string TagTooShort = "Taggable.Tag.TooShort";
        public const string TagTooLong = "Taggable.Tag.TooLong";
        public const string TagInvalid = "Taggable.Tag.Invalid";
        public const string TagDuplicate = "Taggable.Tag.Duplicate";
        public const string TagNotFound = "Taggable.Tag.NotFound";
        public const string TagsTooMany = "Taggable.Tags.TooMany";
    }
}