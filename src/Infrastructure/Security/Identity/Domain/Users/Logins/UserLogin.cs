using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Identity.Domain.Users.Logins;

/// <summary>
/// Represents an external login (e.g., Google, Microsoft) associated with a user.
/// Inherits from <see cref="IdentityUserLogin{TKey}"/>.
/// </summary>
public class UserLogin : IdentityUserLogin<Guid>
{
    /// <summary>
    /// Required for EF Core and Identity instantiation.
    /// </summary>
    public UserLogin()
    {
    }

    /// <summary>
    /// Gets or sets the user navigation property.
    /// </summary>
    public User User { get; set; } = null!;
}
