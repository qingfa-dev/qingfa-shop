using Application.Common.Security.Authentication.Identity;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;

using Infrastructure.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Identity.Services;

/// <summary>Issues access/refresh token pairs for already-authenticated users.</summary>
public sealed class IdentityTokenIssuer(
    UserManager<User> userManager,
    ITokenService tokenService,
    IRefreshTokenService refreshTokens,
    TimeProvider clock)
{
    /// <summary>Issues a new token pair for the user.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token pair and user projection, or a failure.</returns>
    public async Task<Result<AuthResponse>> IssueAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        Result<AccessToken> accessToken = tokenService.CreateAccessToken(
            new AccessTokenRequest(
                user.Id.ToString(),
                user.UserName,
                user.Email,
                TenantId: null,
                Roles: [.. roles]));

        if (accessToken.IsFailure)
            return Result<AuthResponse>.Fail(accessToken.Errors);

        Result<RefreshTokenIssue> refreshToken = await refreshTokens
            .IssueAsync(user.Id.ToString(), clock.GetUtcNow(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (refreshToken.IsFailure)
            return Result<AuthResponse>.Fail(refreshToken.Errors);

        return Result<AuthResponse>.Ok(
            new AuthResponse(
                accessToken.Value,
                refreshToken.Value,
                IdentityUserProjection.ToModel(user)));
    }
}
