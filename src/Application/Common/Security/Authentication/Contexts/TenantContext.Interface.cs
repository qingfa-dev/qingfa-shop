namespace Application.Common.Security.Authentication.Contexts;

/// <summary>Supplies the tenant that owns the current request.</summary>
/// <remarks>
/// The host populates the implementation; handlers only read it. Commands never
/// accept a tenant identifier, so a caller cannot act on another tenant's data.
/// </remarks>
public interface ITenantContext
{
    /// <summary>Gets the current tenant identifier, or <see cref="Guid.Empty"/> when unset.</summary>
    Guid TenantId { get; }
}
