namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Referenceable;

/// <summary>Defines failure errors for the Referenceable concern.</summary>
public static class ReferenceableOutcome
{
    /// <summary>Validation error codes for reference validation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Referenceable entity is required.");

        /// <summary>Error when the reference is missing or whitespace.</summary>
        public static Error ReferenceRequired => Error.UnprocessableEntity(
            code: Codes.ReferenceRequired,
            message:
                $"Reference must be specified ({ReferenceableConstant.Constraints.Reference.MinLength}-{ReferenceableConstant.Constraints.Reference.MaxLength} characters).");

        /// <summary>Error when the reference exceeds the maximum length.</summary>
        public static Error ReferenceTooLong => Error.UnprocessableEntity(
            code: Codes.ReferenceTooLong,
            message:
                $"Reference cannot exceed {ReferenceableConstant.Constraints.Reference.MaxLength} characters.");

        /// <summary>Error when the reference has leading or trailing whitespace.</summary>
        public static Error ReferenceUntrimmed => Error.UnprocessableEntity(
            code: Codes.ReferenceUntrimmed,
            message: "Reference must not have leading or trailing whitespace.");

        /// <summary>Error when the reference uses characters outside the allowed set.</summary>
        public static Error ReferenceInvalidCharset => Error.UnprocessableEntity(
            code: Codes.ReferenceInvalidCharset,
            message: "Reference contains invalid characters. Allowed characters: letters, digits, space, '-', '_', '/', '#', '.', ':'.");
        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Referenceable.Entity.Required";
        public const string ReferenceRequired = "Referenceable.Reference.Required";
        public const string ReferenceTooLong = "Referenceable.Reference.TooLong";
        public const string ReferenceUntrimmed = "Referenceable.Reference.Untrimmed";
        public const string ReferenceInvalidCharset = "Referenceable.Reference.InvalidCharset";
    }
}