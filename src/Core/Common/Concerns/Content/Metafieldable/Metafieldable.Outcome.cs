namespace Core.Common.Concerns.Content.Metafieldable;

/// <summary>Result constants for the <see cref="Metafieldable"/> concern.</summary>
public static class MetafieldableOutcome
{
    /// <summary>Failure error constants for the <see cref="Metafieldable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the metafieldable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Metafieldable.Entity.Required",
            message: "Metafieldable entity is required.");

        /// <summary>Error when the namespace is not specified.</summary>
        public static Error NamespaceRequired => Error.UnprocessableEntity(
            code: "Metafieldable.Namespace.Required",
            message: "Metafield namespace must be specified.");

        /// <summary>Error when the namespace is shorter than the minimum length.</summary>
        public static Error NamespaceTooShort => Error.UnprocessableEntity(
            code: "Metafieldable.Namespace.TooShort",
            message:
                $"Namespace must be at least {MetafieldableConstant.Constraints.Namespace.MinLength} character(s).");

        /// <summary>Error when the namespace exceeds the maximum length.</summary>
        public static Error NamespaceTooLong => Error.UnprocessableEntity(
            code: "Metafieldable.Namespace.TooLong",
            message:
                $"Namespace cannot exceed {MetafieldableConstant.Constraints.Namespace.MaxLength} characters.");

        /// <summary>Error when the namespace is not valid snake_case.</summary>
        public static Error NamespaceInvalid => Error.UnprocessableEntity(
            code: "Metafieldable.Namespace.Invalid",
            message:
                "Namespace must be snake_case (lowercase letters, digits, underscores).");

        /// <summary>Error when the key is not specified.</summary>
        public static Error KeyRequired => Error.UnprocessableEntity(
            code: "Metafieldable.Key.Required",
            message: "Metafield key must be specified.");

        /// <summary>Error when the key is shorter than the minimum length.</summary>
        public static Error KeyTooShort => Error.UnprocessableEntity(
            code: "Metafieldable.Key.TooShort",
            message:
                $"Key must be at least {MetafieldableConstant.Constraints.Key.MinLength} character(s).");

        /// <summary>Error when the key exceeds the maximum length.</summary>
        public static Error KeyTooLong => Error.UnprocessableEntity(
            code: "Metafieldable.Key.TooLong",
            message:
                $"Key cannot exceed {MetafieldableConstant.Constraints.Key.MaxLength} characters.");

        /// <summary>Error when the key is not valid snake_case.</summary>
        public static Error KeyInvalid => Error.UnprocessableEntity(
            code: "Metafieldable.Key.Invalid",
            message:
                "Key must be snake_case (lowercase letters, digits, underscores).");

        /// <summary>Error when the metafield value exceeds the maximum length.</summary>
        public static Error ValueTooLong => Error.UnprocessableEntity(
            code: "Metafieldable.Value.TooLong",
            message:
                $"Metafield value cannot exceed {MetafieldableConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error when the metafield is not found.</summary>
        public static Error MetafieldNotFound => Error.UnprocessableEntity(
            code: "Metafieldable.Metafield.NotFound",
            message: "Metafield is not present.");

        /// <summary>Error when the entity already has the maximum number of metafields.</summary>
        public static Error MetafieldsTooMany => Error.UnprocessableEntity(
            code: "Metafieldable.Metafield.TooMany",
            message:
                $"Cannot store more than {MetafieldableConstant.Constraints.Collection.MaxMetafields} metafields.");

        #endregion
    }
}