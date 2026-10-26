namespace Core.Common.Concerns.Foundation.StoreScoped;

/// <summary>Defines failure errors for the StoreScoped concern.</summary>
public static class StoreScopedOutcome
{
    /// <summary>Validation error codes for store-scoped validation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "StoreScoped.Entity.Required",
            message: "Store-scoped entity is required.");

        /// <summary>Error when the store identifier is the default value.</summary>
        public static Error StoreIdRequired => Error.UnprocessableEntity(
            code: "StoreScoped.StoreId.Required",
            message: "Store identifier must be specified (default values are not allowed).");

        #endregion
    }
}