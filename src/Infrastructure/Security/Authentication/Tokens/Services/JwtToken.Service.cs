using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;
using Application.Common.Security.Authentication.Tokens.Options;

using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.Authentication.Tokens.Services;

/// <summary>Issues HMAC-signed JWT access tokens from validated application options.</summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtTokenOption _option;
    private readonly SigningCredentials _credentials;
    private readonly TimeProvider _clock;
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>Initializes the service with its settings and clock.</summary>
    /// <param name="option">The JWT settings.</param>
    /// <param name="clock">The clock used to stamp issuance and expiry.</param>
    /// <exception cref="ArgumentNullException">A dependency is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The settings are invalid.</exception>
    public JwtTokenService(JwtTokenOption option, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(clock);

        Result<JwtTokenOption> validation = TokenOptionValidator.Validate(option);
        if (validation.IsFailure)
        {
            throw new InvalidOperationException(
                string.Join("; ", validation.Errors.Select(error => error.Message)));
        }

        _option = validation.Value;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_option.Key)),
            TokenConstant.Scheme.HmacSha256);
    }

    /// <inheritdoc />
    public Result<AccessToken> CreateAccessToken(AccessTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<AccessTokenRequest> validation = TokenOptionValidator.Validate(request);
        if (validation.IsFailure)
            return Result<AccessToken>.Fail(validation.Errors);

        DateTimeOffset issuedAt = _clock.GetUtcNow();
        DateTimeOffset expiresAt = issuedAt.AddMinutes(_option.AccessTokenMinutes);
        string jti = Guid.NewGuid().ToString("N");

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, request.UserId.Trim()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        ];

        if (!string.IsNullOrWhiteSpace(request.UserName))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, request.UserName.Trim()));

        if (!string.IsNullOrWhiteSpace(request.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email.Trim()));

        if (request.TenantId is not null)
        {
            claims.Add(new Claim(
                TokenConstant.Claims.TenantId,
                request.TenantId.Value.ToString()));
        }

        if (request.Roles is not null)
        {
            foreach (string role in request.Roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                    claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (request.Claims is not null)
        {
            foreach ((string key, string value) in request.Claims)
                claims.Add(new Claim(key, value));
        }

        var token = new JwtSecurityToken(
            issuer: _option.Issuer,
            audience: _option.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        try
        {
            string value = _handler.WriteToken(token);
            return Result<AccessToken>.Ok(
                new AccessToken(value, TokenConstant.Scheme.Bearer, issuedAt, expiresAt, jti));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            // Handle: Signing failures are infrastructure failures, not caller validation errors.
            return Result<AccessToken>.Fail(TokenOutcome.Failure.AccessTokenGenerationFailed);
        }
    }
}
