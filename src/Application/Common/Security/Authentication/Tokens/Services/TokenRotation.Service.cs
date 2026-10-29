using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Common.Security.Authentication.Tokens.Services;

/// <summary>
/// Rotates refresh tokens and converts reuse of a replaced token into theft detection.
/// </summary>
public sealed class TokenRotationService : ITokenRotationService
{
    private readonly IRefreshTokenService _refreshTokens;

    /// <summary>Initializes the service with the refresh-token service it coordinates.</summary>
    /// <param name="refreshTokens">The refresh-token service.</param>
    /// <exception cref="ArgumentNullException"><paramref name="refreshTokens"/> is <c>null</c>.</exception>
    public TokenRotationService(IRefreshTokenService refreshTokens)
    {
        ArgumentNullException.ThrowIfNull(refreshTokens);
        _refreshTokens = refreshTokens;
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenIssue>> RotateAsync(
        string refreshToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        Result<RefreshToken> found = await _refreshTokens
            .FindAsync(refreshToken, cancellationToken)
            .ConfigureAwait(false);

        if (found.IsFailure)
            return Result<RefreshTokenIssue>.Fail(found.Errors);

        RefreshToken current = found.Value;

        // Handle: Reuse of a rotated token is theft; mark it and fail closed.
        if (current.IsRevoked)
        {
            if (current.RevocationReason == RefreshTokenRevocationReason.Replaced)
            {
                Result marked = await _refreshTokens
                    .MarkReuseAsync(current, nowUtc, cancellationToken)
                    .ConfigureAwait(false);

                if (marked.IsFailure)
                    return Result<RefreshTokenIssue>.Fail(marked.Errors);

                // Enforce: Reuse compromises the whole family, so active user tokens are revoked.
                Result<int> revoked = await _refreshTokens
                    .RevokeAllAsync(
                        current.UserId,
                        nowUtc,
                        RefreshTokenRevocationReason.ReuseDetected,
                        cancellationToken)
                    .ConfigureAwait(false);

                return revoked.IsSuccess
                    ? Result<RefreshTokenIssue>.Fail(RefreshTokenOutcome.Failure.TheftDetected)
                    : Result<RefreshTokenIssue>.Fail(revoked.Errors);
            }

            return Result<RefreshTokenIssue>.Fail(RefreshTokenOutcome.Failure.Revoked);
        }

        if (current.IsExpired(nowUtc))
            return Result<RefreshTokenIssue>.Fail(RefreshTokenOutcome.Failure.Expired);

        return await _refreshTokens
            .RotateAsync(current, nowUtc, cancellationToken)
            .ConfigureAwait(false);
    }
}
