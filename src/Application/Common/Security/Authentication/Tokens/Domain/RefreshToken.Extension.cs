namespace Application.Common.Security.Authentication.Tokens.Domain;

/// <summary>Lifecycle helpers for <see cref="RefreshToken"/> results.</summary>
public static class RefreshTokenExtension
{
    #region Public Methods

    /// <summary>Fails when the token is revoked or expired at the supplied moment.</summary>
    /// <param name="result">The result carrying the token.</param>
    /// <param name="nowUtc">The moment used to evaluate expiry.</param>
    /// <returns>The original result when the token is active; otherwise a failure.</returns>
    public static Result<RefreshToken> EnsureActive(
        this Result<RefreshToken> result,
        DateTimeOffset nowUtc)
    {
        return result
            .Ensure(token => !token.IsRevoked, RefreshTokenOutcome.Failure.Revoked)
            .Ensure(token => !token.IsExpired(nowUtc), RefreshTokenOutcome.Failure.Expired);
    }

    /// <summary>Revokes an active or expired token, preserving the first revocation.</summary>
    /// <param name="result">The result carrying the token.</param>
    /// <param name="revokedAtUtc">The moment of revocation; converted to UTC.</param>
    /// <param name="reason">The reason for the revocation.</param>
    /// <returns>The result with the token revoked on success; otherwise a failure.</returns>
    public static Result<RefreshToken> Revoke(
        this Result<RefreshToken> result,
        DateTimeOffset revokedAtUtc,
        RefreshTokenRevocationReason reason)
    {
        DateTimeOffset normalized = revokedAtUtc.ToUniversalTime();

        return result
            .Ensure(token => RefreshTokenValidator.ValidateRevocationReason(reason, normalized))
            .Ensure(token => RefreshTokenValidator.ValidateRevokedAt(normalized, token.CreatedAtUtc))
            .Tap(token =>
            {
                // Handle: Revocation is idempotent; the first reason wins.
                if (token.IsRevoked)
                    return;

                token.ApplyRevocation(normalized, reason);
            });
    }

    /// <summary>Marks the token as reused after rotation and records the theft reason.</summary>
    /// <param name="result">The result carrying the token.</param>
    /// <param name="detectedAtUtc">The moment reuse was detected; converted to UTC.</param>
    /// <returns>The result with the token marked on success; otherwise a failure.</returns>
    public static Result<RefreshToken> DetectReuse(
        this Result<RefreshToken> result,
        DateTimeOffset detectedAtUtc)
    {
        DateTimeOffset normalized = detectedAtUtc.ToUniversalTime();

        return result
            .Ensure(token => RefreshTokenValidator.ValidateRevokedAt(normalized, token.CreatedAtUtc))
            .Tap(token =>
            {
                // Handle: Reuse detection is idempotent; theft overrides earlier revocation reasons.
                if (token.RevocationReason == RefreshTokenRevocationReason.ReuseDetected)
                    return;

                token.ApplyRevocation(normalized, RefreshTokenRevocationReason.ReuseDetected);
            });
    }

    /// <summary>Rotates an active token into its replacement within the same family.</summary>
    /// <param name="result">The result carrying the current token.</param>
    /// <param name="replacement">The newly issued token.</param>
    /// <param name="nowUtc">The moment of rotation; converted to UTC.</param>
    /// <returns>The result with the current token revoked on success; otherwise a failure.</returns>
    public static Result<RefreshToken> ReplaceWith(
        this Result<RefreshToken> result,
        RefreshToken? replacement,
        DateTimeOffset nowUtc)
    {
        DateTimeOffset normalized = nowUtc.ToUniversalTime();

        return result
            .EnsureActive(normalized)
            .Ensure(token => replacement is not null, RefreshTokenOutcome.Failure.ReplacementRequired)
            .Ensure(
                token => replacement!.Id != token.Id,
                RefreshTokenOutcome.Failure.ReplacementSameToken)
            .Ensure(
                token => replacement!.TokenFamilyId == token.TokenFamilyId,
                RefreshTokenOutcome.Failure.ReplacementFamilyMismatch)
            .Ensure(
                token => string.Equals(replacement!.UserId, token.UserId, StringComparison.Ordinal),
                RefreshTokenOutcome.Failure.ReplacementUserMismatch)
            .Ensure(
                token => replacement!.IsActive(normalized),
                RefreshTokenOutcome.Failure.ReplacementNotActive)
            .Tap(token =>
            {
                token.ApplyReplacement(replacement!);
                token.ApplyRevocation(normalized, RefreshTokenRevocationReason.Replaced);
            });
    }

    #endregion
}
