namespace Core.Common.Concerns.Foundation.Tenantable;

/// <summary>Defines failure errors for the Tenantable concern.</summary>
public static class TenantableOutcome
{
    /// <summary>Validation error codes for tenancy validation.</summary>
    public static class Failure
    {
        #region Validation

        /// <summary>Error when the entity itself is missing.</summary>
        public static Error EntityRequired => Error.UnprocessableEntity(
            code: "Tenantable.Entity.Required",
            message: "Tenantable entity is required.");

        /// <summary>Error when the tenant identifier is the default value.</summary>
        public static Error TenantIdRequired => Error.UnprocessableEntity(
            code: "Tenantable.TenantId.Required",
            message: "Tenant identifier must be specified (default values are not allowed).");

        #endregion
    }
}