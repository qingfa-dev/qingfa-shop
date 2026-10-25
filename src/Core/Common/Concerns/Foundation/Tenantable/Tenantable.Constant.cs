namespace Core.Common.Concerns.Foundation.Tenantable;

/// <summary>Constants for the Tenantable concern.</summary>
public static class TenantableConstant
{
    /// <summary>Constraint values for tenancy validation.</summary>
    public static class Constraints
    {
        /// <summary>Tenant identifier constraints.</summary>
        public static class Tenant
        {
            /// <summary>Whether the default value for the tenant key type is allowed. Always false.</summary>
            public const bool AllowDefault = false;
        }
    }

    /// <summary>Default values for the Tenantable concern.</summary>
    public static class Defaults
    {
        /// <summary>Empty tenant identifier sentinel for <see cref="Guid"/> keys.</summary>
        public static readonly Guid EmptyTenantId = Guid.Empty;
    }
}