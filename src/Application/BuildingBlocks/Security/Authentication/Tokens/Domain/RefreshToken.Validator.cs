using Core.BuildingBlocks.Modeling.Concerns.Foundation.Identifiable;

namespace Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

/// <summary>Validates the invariants for <see cref="IRefreshToken"/> without mutating it.</summary>
public static class RefreshTokenValidator
{
    #region Public Methods

    /// <summary>Validates a refresh token against every rule it must always satisfy.</summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>Success when every rule holds; otherwise a failure with the first broken rule.</returns>
    public static Result<RefreshToken> Validate(RefreshToken? token)
    {
        // Guard: Reject a missing token immediately.
        if (token is null)
        {
            return Result<RefreshToken>.Fail(
                RefreshTokenOutcome.Failure.EntityRequired);
        }

        return Result<RefreshToken>.Success(token)
            .Bind(entity => IdentifiableValidator.ValidateIdentification<RefreshToken, Guid>(entity))
            .Ensure(entity => ValidateTokenHash(entity.TokenHash))
            .Ensure(entity => ValidateUserId(entity.UserId))
            .Ensure(entity => ValidateTokenFamilyId(entity.TokenFamilyId))
            .Ensure(entity => ValidateCreatedAt(entity.CreatedAtUtc))
            .Ensure(entity => ValidateExpiresAt(entity.ExpiresAtUtc, entity.CreatedAtUtc))
            .Ensure(entity => ValidateRevokedAt(entity.RevokedAtUtc, entity.CreatedAtUtc))
            .Ensure(entity => ValidateRevocationReason(entity.RevocationReason, entity.RevokedAtUtc))
            .Ensure(entity => ValidateReplacement(
                entity.ReplacedByToken,
                entity.Id,
                entity.UserId,
                entity.TokenFamilyId));
    }

    /// <summary>Validates the stored token hash.</summary>
    /// <param name="tokenHash">The hash to validate.</param>
    /// <returns>Success when the hash is usable; otherwise a failure.</returns>
    public static Result ValidateTokenHash(string? tokenHash)
    {
        // Guard: The hash must be specified.
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.TokenHashRequired);
        }

        // Guard: The hash must not exceed the maximum length.
        if (tokenHash.Length > RefreshTokenConstant.Constraints.TokenHash.MaxLength)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.TokenHashTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates the owning user identifier.</summary>
    /// <param name="userId">The user identifier to validate.</param>
    /// <returns>Success when the identifier is usable; otherwise a failure.</returns>
    public static Result ValidateUserId(string? userId)
    {
        // Guard: The user identifier must be specified.
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.UserIdRequired);
        }

        // Guard: The user identifier must not exceed the maximum length.
        if (userId.Length > RefreshTokenConstant.Constraints.UserId.MaxLength)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.UserIdTooLong);
        }

        return Result.Success();
    }

    /// <summary>Validates the token family identifier.</summary>
    /// <param name="tokenFamilyId">The family identifier to validate.</param>
    /// <returns>Success when the identifier is usable; otherwise a failure.</returns>
    public static Result ValidateTokenFamilyId(Guid tokenFamilyId)
    {
        // Guard: The family identifier must not be the empty sentinel.
        if (tokenFamilyId == Guid.Empty)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.TokenFamilyIdRequired);
        }

        return Result.Success();
    }

    /// <summary>Validates the creation timestamp.</summary>
    /// <param name="createdAtUtc">The creation timestamp to validate.</param>
    /// <returns>Success when the timestamp is usable; otherwise a failure.</returns>
    public static Result ValidateCreatedAt(DateTimeOffset createdAtUtc)
    {
        // Guard: The creation timestamp must be specified.
        if (createdAtUtc == default)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.CreatedAtRequired);
        }

        return Result.Success();
    }

    /// <summary>Validates the expiry timestamp against the creation timestamp.</summary>
    /// <param name="expiresAtUtc">The expiry timestamp to validate.</param>
    /// <param name="createdAtUtc">The creation timestamp.</param>
    /// <returns>Success when the expiry is usable; otherwise a failure.</returns>
    public static Result ValidateExpiresAt(
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        // Guard: The expiry timestamp must be specified.
        if (expiresAtUtc == default)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.ExpiresAtRequired);
        }

        // Guard: The expiry must be strictly later than the creation timestamp.
        if (createdAtUtc != default && expiresAtUtc <= createdAtUtc)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.ExpiresAtBeforeCreated);
        }

        return Result.Success();
    }

    /// <summary>Validates a revocation timestamp against the creation timestamp.</summary>
    /// <param name="revokedAtUtc">The optional revocation timestamp.</param>
    /// <param name="createdAtUtc">The creation timestamp.</param>
    /// <returns>Success when the revocation timestamp is usable; otherwise a failure.</returns>
    public static Result ValidateRevokedAt(
        DateTimeOffset? revokedAtUtc,
        DateTimeOffset createdAtUtc)
    {
        // Guard: No revocation timestamp means there is nothing to compare.
        if (revokedAtUtc is null)
        {
            return Result.Success();
        }

        // Guard: A revocation cannot precede the token's creation.
        if (createdAtUtc != default && revokedAtUtc.Value < createdAtUtc)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.RevokedAtBeforeCreated);
        }

        return Result.Success();
    }

    /// <summary>Validates the revocation reason and its pairing with the revocation timestamp.</summary>
    /// <param name="reason">The optional revocation reason.</param>
    /// <param name="revokedAtUtc">The optional revocation timestamp.</param>
    /// <returns>Success when the revocation state is consistent; otherwise a failure.</returns>
    public static Result ValidateRevocationReason(
        RefreshTokenRevocationReason? reason,
        DateTimeOffset? revokedAtUtc)
    {
        // Guard: A revocation must explain why it happened.
        if (reason is null)
        {
            return revokedAtUtc is null
                ? Result.Success()
                : Result.Fail(RefreshTokenOutcome.Failure.RevocationReasonRequired);
        }

        // Guard: The reason must be a defined enum value.
        if (!Enum.IsDefined(reason.Value))
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.RevocationReasonInvalid);
        }

        // Guard: A reason without a revocation timestamp describes an impossible state.
        if (revokedAtUtc is null)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.RevocationReasonWithoutRevocation);
        }

        return Result.Success();
    }

    /// <summary>Validates the replacement link created during rotation.</summary>
    /// <param name="replacement">The optional replacement token.</param>
    /// <param name="tokenId">The identifier of the token being replaced.</param>
    /// <param name="tokenUserId">The owning user of the token being replaced.</param>
    /// <param name="tokenFamilyId">The family identifier of the token being replaced.</param>
    /// <returns>Success when the replacement link is usable; otherwise a failure.</returns>
    public static Result ValidateReplacement(
        RefreshToken? replacement,
        Guid tokenId,
        string? tokenUserId,
        Guid tokenFamilyId)
    {
        if (replacement is null)
        {
            return Result.Success();
        }

        // Guard: A token cannot replace itself.
        if (replacement.Id == tokenId)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.ReplacementSameToken);
        }

        // Guard: Rotation must stay inside the same token family.
        if (replacement.TokenFamilyId != tokenFamilyId)
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.ReplacementFamilyMismatch);
        }

        // Guard: A token family cannot migrate to another user during rotation.
        if (!string.Equals(replacement.UserId, tokenUserId, StringComparison.Ordinal))
        {
            return Result.Fail(
                RefreshTokenOutcome.Failure.ReplacementUserMismatch);
        }

        return Result.Success();
    }

    #endregion
}
