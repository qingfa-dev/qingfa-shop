using Microsoft.AspNetCore.Identity;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles.Claims;

/// <summary>
/// Represents a claim assigned to a role, defining permissions for all users in that role.
/// Inherits from <see cref="IdentityRoleClaim{TKey}"/> but implements project-specific metadata and guards
/// to ensure ERP-level auditability and data integrity.
/// </summary>
public class RoleClaim : IdentityRoleClaim<Guid>
{
    // Navigation

    /// <summary>
    /// Gets or sets the role navigation property.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Required for EF Core and Identity instantiation.
    /// </summary>
    public RoleClaim() { }
}
