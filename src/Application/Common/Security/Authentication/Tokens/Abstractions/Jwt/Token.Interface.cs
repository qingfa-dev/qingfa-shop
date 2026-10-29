namespace Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;

/// <summary>Issues signed JWT access tokens.</summary>
public interface ITokenService
{
    /// <summary>Creates a signed access token for the supplied user.</summary>
    /// <param name="request">The user, tenant, role, and claim data to embed.</param>
    /// <returns>The signed token and its metadata, or a configuration/validation failure.</returns>
    Result<AccessToken> CreateAccessToken(AccessTokenRequest request);
}
