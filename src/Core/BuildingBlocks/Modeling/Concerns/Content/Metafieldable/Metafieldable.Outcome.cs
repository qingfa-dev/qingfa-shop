namespace Core.BuildingBlocks.Modeling.Concerns.Content.Metafieldable;

/// <summary>Result constants for the <see cref="Metafieldable"/> concern.</summary>
public static class MetafieldableOutcome
{
    /// <summary>Failure error constants for the <see cref="Metafieldable"/> concern.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the metafieldable entity is required but missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Metafieldable entity is required.");

        /// <summary>Error when the namespace is not specified.</summary>
        public static Error NamespaceRequired => Error.UnprocessableEntity(
            code: Codes.NamespaceRequired,
            message: "Metafield namespace must be specified.");

        /// <summary>Error when the namespace is shorter than the minimum length.</summary>
        public static Error NamespaceTooShort => Error.UnprocessableEntity(
            code: Codes.NamespaceTooShort,
            message:
                $"Namespace must be at least {MetafieldableConstant.Constraints.Namespace.MinLength} character(s).");

        /// <summary>Error when the namespace exceeds the maximum length.</summary>
        public static Error NamespaceTooLong => Error.UnprocessableEntity(
            code: Codes.NamespaceTooLong,
            message:
                $"Namespace cannot exceed {MetafieldableConstant.Constraints.Namespace.MaxLength} characters.");

        /// <summary>Error when the namespace is not valid snake_case.</summary>
        public static Error NamespaceInvalid => Error.UnprocessableEntity(
            code: Codes.NamespaceInvalid,
            message:
                "Namespace must be snake_case (lowercase letters, digits, underscores).");

        /// <summary>Error when the key is not specified.</summary>
        public static Error KeyRequired => Error.UnprocessableEntity(
            code: Codes.KeyRequired,
            message: "Metafield key must be specified.");

        /// <summary>Error when the key is shorter than the minimum length.</summary>
        public static Error KeyTooShort => Error.UnprocessableEntity(
            code: Codes.KeyTooShort,
            message:
                $"Key must be at least {MetafieldableConstant.Constraints.Key.MinLength} character(s).");

        /// <summary>Error when the key exceeds the maximum length.</summary>
        public static Error KeyTooLong => Error.UnprocessableEntity(
            code: Codes.KeyTooLong,
            message:
                $"Key cannot exceed {MetafieldableConstant.Constraints.Key.MaxLength} characters.");

        /// <summary>Error when the key is not valid snake_case.</summary>
        public static Error KeyInvalid => Error.UnprocessableEntity(
            code: Codes.KeyInvalid,
            message:
                "Key must be snake_case (lowercase letters, digits, underscores).");

        /// <summary>Error when the metafield value exceeds the maximum length.</summary>
        public static Error ValueTooLong => Error.UnprocessableEntity(
            code: Codes.ValueTooLong,
            message:
                $"Metafield value cannot exceed {MetafieldableConstant.Constraints.Value.MaxLength} characters.");

        /// <summary>Error when the metafield is not found.</summary>
        public static Error MetafieldNotFound => Error.UnprocessableEntity(
            code: Codes.MetafieldNotFound,
            message: "Metafield is not present.");

        /// <summary>Error when the entity already has the maximum number of metafields.</summary>
        public static Error MetafieldsTooMany => Error.UnprocessableEntity(
            code: Codes.MetafieldsTooMany,
            message:
                $"Cannot store more than {MetafieldableConstant.Constraints.Collection.MaxMetafields} metafields.");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Metafieldable.Entity.Required";
        public const string NamespaceRequired = "Metafieldable.Namespace.Required";
        public const string NamespaceTooShort = "Metafieldable.Namespace.TooShort";
        public const string NamespaceTooLong = "Metafieldable.Namespace.TooLong";
        public const string NamespaceInvalid = "Metafieldable.Namespace.Invalid";
        public const string KeyRequired = "Metafieldable.Key.Required";
        public const string KeyTooShort = "Metafieldable.Key.TooShort";
        public const string KeyTooLong = "Metafieldable.Key.TooLong";
        public const string KeyInvalid = "Metafieldable.Key.Invalid";
        public const string ValueTooLong = "Metafieldable.Value.TooLong";
        public const string MetafieldNotFound = "Metafieldable.Metafield.NotFound";
        public const string MetafieldsTooMany = "Metafieldable.Metafield.TooMany";
    }
}