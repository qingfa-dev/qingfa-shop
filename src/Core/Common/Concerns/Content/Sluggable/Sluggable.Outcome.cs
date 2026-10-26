namespace Core.Common.Concerns.Content.Sluggable;

/// <summary>Result constants for the <see cref="Sluggable"/> concern.</summary>
public static class SluggableOutcome
{
    /// <summary>Failure error constants for the <see cref="Sluggable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the sluggable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Sluggable.Entity.Required",
            message: "Sluggable entity is required.");

        /// <summary>Error when the slug is not specified.</summary>
        public static Error SlugRequired => Error.UnprocessableEntity(
            code: "Sluggable.Slug.Required",
            message: "Slug must be specified.");

        /// <summary>Error when the slug is shorter than the minimum length.</summary>
        public static Error SlugTooShort => Error.UnprocessableEntity(
            code: "Sluggable.Slug.TooShort",
            message:
                $"Slug must be at least {SluggableConstant.Constraints.Slug.MinLength} character(s).");

        /// <summary>Error when the slug exceeds the maximum length.</summary>
        public static Error SlugTooLong => Error.UnprocessableEntity(
            code: "Sluggable.Slug.TooLong",
            message:
                $"Slug cannot exceed {SluggableConstant.Constraints.Slug.MaxLength} characters.");

        /// <summary>Error when the slug is not valid.</summary>
        public static Error SlugInvalid => Error.UnprocessableEntity(
            code: "Sluggable.Slug.Invalid",
            message:
                "Slug must contain only lowercase letters, digits and single hyphens.");

        #endregion
    }
}