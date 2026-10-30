using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Features.Identity.External.Models;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

namespace Web.Api.Endpoints.Identity.Mappings;

/// <summary>Maps an authenticated external principal onto the application user information.</summary>
public static class ExternalPrincipalMapper
{
    /// <summary>Maps the provider principal claims onto the normalized external user.</summary>
    /// <param name="provider">The authentication scheme that produced the principal.</param>
    /// <param name="principal">The external principal.</param>
    /// <returns>The normalized external user, or a 401 failure when no subject claim exists.</returns>
    public static Result<ExternalUserInfo> ToExternalUserInfo(
        string provider,
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? providerKey = FindFirstClaim(
            principal,
            ClaimTypes.NameIdentifier,
            JwtRegisteredClaimNames.Sub);

        return string.IsNullOrWhiteSpace(providerKey)
            ? Result<ExternalUserInfo>.Fail(Error.Unauthorized(
                "Auth.External.SubjectMissing",
                "The external provider did not return a subject identifier."))
            : Result<ExternalUserInfo>.Ok(new ExternalUserInfo(
                provider,
                providerKey,
                FindFirstClaim(principal, ClaimTypes.Name, "name"),
                FindFirstClaim(principal, ClaimTypes.Email, "email")));
    }

    private static string? FindFirstClaim(ClaimsPrincipal principal, params string[] claimTypes) =>
        claimTypes
            .Select(principal.FindFirstValue)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
