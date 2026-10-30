using ApplicationIdentity = Application.Common.Security.Authentication.Identity;

namespace Web.Api.Endpoints.Identity.Mappings;

/// <summary>Projects application identity responses onto the public wire model.</summary>
public static class IdentityMapper
{
    /// <summary>Maps an issued token pair and user onto the serializable response.</summary>
    /// <param name="response">The application auth response.</param>
    /// <returns>The public token response.</returns>
    public static AuthTokenResponse ToTokenResponse(ApplicationIdentity.AuthResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new AuthTokenResponse(
            response.AccessToken.Token,
            response.AccessToken.ExpiresAtUtc,
            response.RefreshToken.Token,
            response.User);
    }
}
