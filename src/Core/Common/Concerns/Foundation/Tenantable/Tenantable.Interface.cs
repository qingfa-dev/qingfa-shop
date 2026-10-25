namespace Core.Common.Concerns.Foundation.Tenantable;

/// <summary>Base concern for entities scoped to a tenant.</summary>
/// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
public interface ITenantable<TTenantKey>
{
    /// <summary>Gets or sets the tenant identifier for the entity.</summary>
    TTenantKey TenantId { get; set; }
}