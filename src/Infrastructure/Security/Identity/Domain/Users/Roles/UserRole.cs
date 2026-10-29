using Microsoft.AspNetCore.Identity;

using Infrastructure.Security.Identity.Domain.Roles;

namespace Infrastructure.Security.Identity.Domain.Users.Roles;

/// <summary>
/// Represents a specific role assigned to a user for fine-grained authorization.
/// Inherits from <see cref="IdentityUserRole{TKey}"/> but implements project-specific metadata and guards
/// to ensure ERP-level auditability and data integrity.
/// </summary>
public class UserRole : IdentityUserRole<Guid>
{
    /// <summary>
    /// Gets or sets the user navigation property.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the role navigation property.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Required for EF Core and Identity instantiation.
    /// </summary>
    public UserRole() { }
}
