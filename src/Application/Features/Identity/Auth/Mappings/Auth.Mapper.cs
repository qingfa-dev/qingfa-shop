using Application.Features.Identity.Auth.Models;

using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Application.Features.Identity.Auth.Mappings;

/// <summary>Projects application identity responses onto the Auth slice wire models.</summary>
public static class AuthMapper
{
    /// <summary>Maps an issued token pair and user onto the serializable response.</summary>
    /// <param name="response">The application auth response.</param>
    /// <returns>The public token response.</returns>
    public static AuthTokenResponse ToResponse(ApplicationIdentity.AuthResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new AuthTokenResponse(
            response.AccessToken.Token,
            response.AccessToken.ExpiresAtUtc,
            response.RefreshToken.Token,
            response.User);
    }

    /// <summary>Maps an identity user onto the requested Auth user response.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="user">The identity user projection.</param>
    /// <returns>The mapped user response.</returns>
    public static T ToUserModel<T>(ApplicationIdentity.IdentityUserModel user)
        where T : AuthUserModel, new()
    {
        ArgumentNullException.ThrowIfNull(user);

        return new T
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
        };
    }

    /// <summary>Maps the two-factor state onto the requested Auth response.</summary>
    /// <typeparam name="T">The response model type.</typeparam>
    /// <param name="info">The two-factor state.</param>
    /// <returns>The mapped two-factor response.</returns>
    public static T ToTwoFactorModel<T>(ApplicationIdentity.TwoFactorInfo info)
        where T : AuthTwoFactorModel, new()
    {
        ArgumentNullException.ThrowIfNull(info);

        return new T
        {
            IsTwoFactorEnabled = info.IsTwoFactorEnabled,
            SharedKey = info.SharedKey,
            AuthenticatorUri = info.AuthenticatorUri,
            RecoveryCodes = [.. info.RecoveryCodes],
        };
    }
}
