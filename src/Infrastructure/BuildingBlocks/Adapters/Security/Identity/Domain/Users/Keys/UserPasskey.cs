using Microsoft.AspNetCore.Identity;


namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Keys;

/// <summary>
/// Represents passkey data for a user in the system.
/// Stores WebAuthn credentials for a user.
/// </summary>
public class UserPasskey : IdentityUserPasskey<Guid>
{
    /// <summary>
    /// Required for EF Core and Identity instantiation.
    /// </summary>
    public UserPasskey()
    {
    }


    /// <summary>
    /// Gets or sets the associated user.
    /// </summary>
    public User User { get; set; } = null!;
}
