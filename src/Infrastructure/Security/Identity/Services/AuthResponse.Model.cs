using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Domain;
using Application.Features.Identity.Auth.Models;

namespace Infrastructure.Security.Identity.Services;

/// <summary>An issued access/refresh token pair and its user, internal to Infrastructure.</summary>
/// <param name="AccessToken">The signed access token.</param>
/// <param name="RefreshToken">The issued refresh token.</param>
/// <param name="User">The authenticated user projection.</param>
internal sealed record AuthResponse(
    AccessToken AccessToken,
    RefreshTokenIssue RefreshToken,
    IdentityUserModel User)
{
    /// <summary>Projects the persisted token pair onto the shared application contract.</summary>
    /// <returns>The public token pair.</returns>
    public AuthTokenResponse ToTokenResponse() => new(
        AccessToken.Token,
        AccessToken.ExpiresAtUtc,
        RefreshToken.Token,
        User);
}
