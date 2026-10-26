namespace Core.Common.Concerns.Foundation.Identifiable;

/// <summary>Defines failure errors for the Identifiable concern.</summary>
public static class IdentifiableOutcome
{
    /// <summary>Validation error codes for identification.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Identifiable.Entity.Required",
            message: "Identifiable entity is required.");

        /// <summary>Error when the identifier is the default value.</summary>
        public static Error IdRequired => Error.UnprocessableEntity(
            code: "Identifiable.Id.Required",
            message: "Identifier must be specified (default values are not allowed).");

        #endregion
    }
}