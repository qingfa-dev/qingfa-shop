namespace Core.BuildingBlocks.Modeling.Concerns.Foundation.Tenantable;

/// <summary>Defines failure errors for the Tenantable concern.</summary>
public static class TenantableOutcome
{
    /// <summary>Validation error codes for tenancy validation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: Codes.EntityRequired,
            message: "Tenantable entity is required.");

        /// <summary>Error when the tenant identifier is the default value.</summary>
        public static Error TenantIdRequired => Error.UnprocessableEntity(
            code: Codes.TenantIdRequired,
            message: "Tenant identifier must be specified (default values are not allowed).");

        #endregion
    }

    /// <summary>Stable error codes for logging, contracts, and tests.</summary>
    public static class Codes
    {
        public const string EntityRequired = "Tenantable.Entity.Required";
        public const string TenantIdRequired = "Tenantable.TenantId.Required";
    }
}