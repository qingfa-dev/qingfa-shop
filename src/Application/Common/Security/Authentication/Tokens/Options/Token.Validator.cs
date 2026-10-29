using System.Security.Claims;

namespace Application.Common.Security.Authentication.Tokens.Options;

/// <summary>Validates token options and service inputs without mutating them.</summary>
public static class TokenOptionValidator
{
    private static readonly HashSet<string> s_reservedClaims = new(StringComparer.Ordinal)
    {
        TokenConstant.Claims.Subject,
        TokenConstant.Claims.Name,
        TokenConstant.Claims.Email,
        TokenConstant.Claims.TenantId,
        TokenConstant.Claims.Jti,
        TokenConstant.Claims.Role,
        TokenConstant.Claims.IssuedAt,
        ClaimTypes.Role
    };

    #region Options

    /// <summary>Validates JWT issuance settings.</summary>
    /// <param name="option">The settings to validate.</param>
    /// <returns>The unchanged settings, or the first broken rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="option"/> is <c>null</c>.</exception>
    public static Result<JwtTokenOption> Validate(JwtTokenOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return Result<JwtTokenOption>.Success(option)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.Key),
                TokenOutcome.Failure.SigningKeyRequired)
            .Ensure(
                value => value.Key.Length >= TokenConstant.Limits.MinSigningKeyLength,
                TokenOutcome.Failure.SigningKeyTooShort)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.Issuer),
                TokenOutcome.Failure.IssuerRequired)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.Audience),
                TokenOutcome.Failure.AudienceRequired)
            .Ensure(
                value => value.AccessTokenMinutes is >= TokenConstant.Limits.MinAccessTokenMinutes
                    and <= TokenConstant.Limits.MaxAccessTokenMinutes,
                TokenOutcome.Failure.AccessTokenLifetimeInvalid)
            .Ensure(
                value => value.ClockSkewSeconds is >= 0
                    and <= TokenConstant.Limits.MaxClockSkewSeconds,
                TokenOutcome.Failure.ClockSkewInvalid);
    }

    /// <summary>Validates refresh-token generation settings.</summary>
    /// <param name="option">The settings to validate.</param>
    /// <returns>The unchanged settings, or the first broken rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="option"/> is <c>null</c>.</exception>
    public static Result<RefreshTokenOption> Validate(RefreshTokenOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return Result<RefreshTokenOption>.Success(option)
            .Ensure(
                value => value.LifetimeDays is >= TokenConstant.Limits.MinRefreshTokenDays
                    and <= TokenConstant.Limits.MaxRefreshTokenDays,
                TokenOutcome.Failure.RefreshTokenLifetimeInvalid)
            .Ensure(
                value => value.TokenByteLength is >= TokenConstant.Limits.MinRefreshTokenBytes
                    and <= TokenConstant.Limits.MaxRefreshTokenBytes,
                TokenOutcome.Failure.RefreshTokenLengthInvalid)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.TokenPrefix),
                TokenOutcome.Failure.RefreshTokenPrefixRequired);
    }

    /// <summary>Validates blacklist settings.</summary>
    /// <param name="option">The settings to validate.</param>
    /// <returns>The unchanged settings, or the first broken rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="option"/> is <c>null</c>.</exception>
    public static Result<TokenBlacklistOption> Validate(TokenBlacklistOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return Result<TokenBlacklistOption>.Success(option)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.KeyPrefix),
                TokenOutcome.Failure.BlacklistPrefixRequired);
    }

    #endregion

    #region Requests

    /// <summary>Validates the data used to issue an access token.</summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The unchanged request, or the first broken rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    public static Result<AccessTokenRequest> Validate(AccessTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Result<AccessTokenRequest>.Success(request)
            .Ensure(
                value => !string.IsNullOrWhiteSpace(value.UserId),
                TokenOutcome.Failure.UserIdRequired)
            .Ensure(
                value => value.UserId.Length <= TokenConstant.Limits.MaxUserIdLength,
                TokenOutcome.Failure.UserIdTooLong)
            .Ensure(
                value => value.UserName is null ||
                    value.UserName.Length <= TokenConstant.Limits.MaxUserNameLength,
                TokenOutcome.Failure.UserNameTooLong)
            .Ensure(
                value => value.Email is null ||
                    value.Email.Length <= TokenConstant.Limits.MaxEmailLength,
                TokenOutcome.Failure.EmailTooLong)
            .Ensure(
                value => value.Claims is null ||
                    value.Claims.Keys.All(key => !string.IsNullOrWhiteSpace(key)),
                TokenOutcome.Failure.ClaimKeyRequired)
            .Ensure(
                value => value.Claims is null ||
                    value.Claims.Keys.All(key => key.Length <= TokenConstant.Limits.MaxClaimKeyLength),
                TokenOutcome.Failure.ClaimKeyTooLong)
            .Ensure(
                value => value.Claims is null ||
                    value.Claims.Keys.All(key => !s_reservedClaims.Contains(key)),
                TokenOutcome.Failure.ClaimKeyReserved);
    }

    /// <summary>Validates an opaque refresh token value.</summary>
    /// <param name="token">The plaintext token to validate.</param>
    /// <returns>Success when the token is usable; otherwise a failure.</returns>
    public static Result ValidateRefreshToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Fail(
                TokenOutcome.Failure.RefreshTokenRequired);
        }

        if (token.Length > TokenConstant.Limits.MaxRefreshTokenLength)
        {
            return Result.Fail(
                TokenOutcome.Failure.RefreshTokenTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates an access-token identifier.</summary>
    /// <param name="jti">The identifier to validate.</param>
    /// <returns>Success when the identifier is usable; otherwise a failure.</returns>
    public static Result ValidateJti(string? jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return Result.Fail(
                TokenOutcome.Failure.JtiRequired);
        }

        if (jti.Length > TokenConstant.Limits.MaxJtiLength)
        {
            return Result.Fail(
                TokenOutcome.Failure.JtiTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates a token owner identifier.</summary>
    /// <param name="userId">The user identifier to validate.</param>
    /// <returns>Success when the identifier is usable; otherwise a failure.</returns>
    public static Result ValidateUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Fail(
                TokenOutcome.Failure.UserIdRequired);
        }

        if (userId.Length > TokenConstant.Limits.MaxUserIdLength)
        {
            return Result.Fail(
                TokenOutcome.Failure.UserIdTooLong);
        }

        return Result.Success();
    }

    #endregion
}
