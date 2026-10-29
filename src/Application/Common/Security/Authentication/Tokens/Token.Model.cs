using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Common.Security.Authentication.Tokens;

/// <summary>The data required to issue an access token.</summary>
/// <param name="UserId">The authenticated user identifier.</param>
/// <param name="UserName">The optional display name.</param>
/// <param name="Email">The optional email address.</param>
/// <param name="TenantId">The optional tenant identifier.</param>
/// <param name="Roles">The optional roles granted to the user.</param>
/// <param name="Claims">Additional claims to include in the token.</param>
public sealed record AccessTokenRequest(
    string UserId,
    string? UserName = null,
    string? Email = null,
    Guid? TenantId = null,
    IReadOnlyCollection<string>? Roles = null,
    IReadOnlyDictionary<string, string>? Claims = null);

/// <summary>A signed JWT access token and its metadata.</summary>
/// <param name="Token">The serialized JWT.</param>
/// <param name="TokenType">The authorization scheme, normally <c>Bearer</c>.</param>
/// <param name="IssuedAtUtc">The UTC moment the token was issued.</param>
/// <param name="ExpiresAtUtc">The UTC moment the token expires.</param>
/// <param name="Jti">The token identifier used for blacklisting.</param>
public sealed record AccessToken(
    string Token,
    string TokenType,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Jti);

/// <summary>An opaque refresh token and the persisted entity that backs it.</summary>
/// <param name="Token">The plaintext refresh token returned to the client.</param>
/// <param name="Entity">The persisted token entity, which stores only the hash.</param>
public sealed record RefreshTokenIssue(string Token, RefreshToken Entity)
{
    /// <summary>Gets the persisted token identifier.</summary>
    public Guid TokenId => Entity.Id;

    /// <summary>Gets the rotation family identifier.</summary>
    public Guid TokenFamilyId => Entity.TokenFamilyId;

    /// <summary>Gets the UTC expiry of the refresh token.</summary>
    public DateTimeOffset ExpiresAtUtc => Entity.ExpiresAtUtc;
}
