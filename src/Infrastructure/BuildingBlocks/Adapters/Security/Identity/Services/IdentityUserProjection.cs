using Application.BuildingBlocks.Security.Identity.Auth.Models;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Services;

/// <summary>Projects identity entities into application response models.</summary>
public static class IdentityUserProjection
{
    /// <summary>Projects a user into the public identity model.</summary>
    /// <param name="user">The user to project.</param>
    /// <returns>The public identity model.</returns>
    public static IdentityUserModel ToModel(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new IdentityUserModel(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.TwoFactorEnabled);
    }
}
