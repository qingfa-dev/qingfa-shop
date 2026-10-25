namespace Core.Common.Concerns.Content.Taggable;

/// <summary>Result constants for the <see cref="Taggable"/> concern.</summary>
public static class TaggableOutcome
{
    /// <summary>Failure error constants for the <see cref="Taggable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the taggable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Taggable.Entity.Required",
            message: "Taggable entity is required.");

        /// <summary>Error when the tag is not specified.</summary>
        public static Error TagRequired => Error.UnprocessableEntity(
            code: "Taggable.Tag.Required",
            message: "Tag must be specified.");

        /// <summary>Error when the tag is shorter than the minimum length.</summary>
        public static Error TagTooShort => Error.UnprocessableEntity(
            code: "Taggable.Tag.TooShort",
            message:
                $"Tag must be at least {TaggableConstant.Constraints.Tag.MinLength} character(s).");

        /// <summary>Error when the tag exceeds the maximum length.</summary>
        public static Error TagTooLong => Error.UnprocessableEntity(
            code: "Taggable.Tag.TooLong",
            message:
                $"Tag cannot exceed {TaggableConstant.Constraints.Tag.MaxLength} characters.");

        /// <summary>Error when the tag has an invalid format.</summary>
        public static Error TagInvalid => Error.UnprocessableEntity(
            code: "Taggable.Tag.Invalid",
            message: "Tag must not have leading or trailing whitespace.");

        /// <summary>Error when the tag is already present.</summary>
        public static Error TagDuplicate => Error.UnprocessableEntity(
            code: "Taggable.Tag.Duplicate",
            message: "Tag is already present.");

        /// <summary>Error when the tag is not found.</summary>
        public static Error TagNotFound => Error.UnprocessableEntity(
            code: "Taggable.Tag.NotFound",
            message: "Tag is not present.");

        /// <summary>Error when the entity already has the maximum number of tags.</summary>
        public static Error TagsTooMany => Error.UnprocessableEntity(
            code: "Taggable.Tags.TooMany",
            message:
                $"Cannot store more than {TaggableConstant.Constraints.Collection.MaxTags} tags.");

        #endregion
    }
}