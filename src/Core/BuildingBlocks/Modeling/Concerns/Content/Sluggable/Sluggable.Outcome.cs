namespace Core.BuildingBlocks.Modeling.Concerns.Content.Sluggable;

/// <summary>Result constants for the <see cref="Sluggable"/> concern.</summary>
public static class SluggableOutcome
{
    /// <summary>Failure error constants for the <see cref="Sluggable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the sluggable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Sluggable entity is required.");

        /// <summary>Error when the slug is not specified.</summary>
        public static Error SlugRequired => Error.UnprocessableEntity(
            code: Codes.SlugRequired,
            message: "Slug must be specified.");

        /// <summary>Error when the slug is shorter than the minimum length.</summary>
        public static Error SlugTooShort => Error.UnprocessableEntity(
            code: Codes.SlugTooShort,
            message:
                $"Slug must be at least {SluggableConstant.Constraints.Slug.MinLength} character(s).");

        /// <summary>Error when the slug exceeds the maximum length.</summary>
        public static Error SlugTooLong => Error.UnprocessableEntity(
            code: Codes.SlugTooLong,
            message:
                $"Slug cannot exceed {SluggableConstant.Constraints.Slug.MaxLength} characters.");

        /// <summary>Error when the slug is not valid.</summary>
        public static Error SlugInvalid => Error.UnprocessableEntity(
            code: Codes.SlugInvalid,
            message:
                "Slug must contain only lowercase letters, digits and single hyphens.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Sluggable.Entity.Required";
        public const string SlugRequired = "Sluggable.Slug.Required";
        public const string SlugTooShort = "Sluggable.Slug.TooShort";
        public const string SlugTooLong = "Sluggable.Slug.TooLong";
        public const string SlugInvalid = "Sluggable.Slug.Invalid";
    }
}