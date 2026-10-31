using Microsoft.AspNetCore.Identity;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Tokens;

/// <summary>
/// Represents a security token assigned to a user.
/// Inherits from <see cref="IdentityUserToken{TKey}"/> but implements project-specific metadata and guards
/// to ensure ERP-level auditability and data integrity.
/// </summary>
public class UserToken : IdentityUserToken<Guid>
{
    /// <summary>
    /// Gets or sets the user navigation property.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Required for EF Core and Identity instantiation.
    /// </summary>
    public UserToken() { }
}
