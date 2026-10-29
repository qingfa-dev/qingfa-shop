using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Identity.Domain.Users.Claims;

/// <summary>
/// Represents a specific claim assigned to a user for fine-grained authorization.
/// Inherits from <see cref="IdentityUserClaim{TKey}"/> but implements project-specific metadata and guards
/// to ensure ERP-level auditability and data integrity.
/// </summary>
public class UserClaim : IdentityUserClaim<Guid>
{
    /// <summary>
    /// Gets or sets the user navigation property.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Required for EF Core and Identity instantiation.
    /// </summary>
    public UserClaim() { }
}
