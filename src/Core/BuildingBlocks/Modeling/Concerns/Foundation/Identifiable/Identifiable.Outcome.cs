namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Identifiable;

/// <summary>Defines failure errors for the Identifiable concern.</summary>
public static class IdentifiableOutcome
{
    /// <summary>Validation error codes for identification.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Identifiable entity is required.");

        /// <summary>Error when the identifier is the default value.</summary>
        public static Error IdRequired => Error.UnprocessableEntity(
            code: Codes.IdRequired,
            message: "Identifier must be specified (default values are not allowed).");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Identifiable.Entity.Required";
        public const string IdRequired = "Identifiable.Id.Required";
    }
}