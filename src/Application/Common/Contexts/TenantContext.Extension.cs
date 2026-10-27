using Core.Common.Concerns.Foundation.Tenantable;

namespace Application.Common.Contexts;

/// <summary>Tenant guard extensions shared by application use cases.</summary>
public static class TenantContextExtension
{
    /// <summary>Starts a chain with the ambient tenant, failing when it is unset.</summary>
    /// <param name="tenantContext">The ambient tenant.</param>
    /// <returns>A result carrying the tenant identifier, or a failure.</returns>
    public static Result<Guid> RequireTenant(this ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        return tenantContext.TenantId == Guid.Empty
            ? Result<Guid>.Fail(TenantableOutcome.Failure.TenantIdRequired)
            : Result<Guid>.Ok(tenantContext.TenantId);
    }
}
