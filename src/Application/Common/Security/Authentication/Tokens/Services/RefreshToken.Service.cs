using System.Security.Cryptography;
using System.Text;

using Application.Common.Security.Authentication.Tokens.Domain;

using Application.Common.Security.Authentication.Tokens.Options;

using Core.Common.Monad.Maybes;

namespace Application.Common.Security.Authentication.Tokens.Services;

/// <summary>
/// Issues and manages opaque refresh tokens while persisting only their hashes.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenStore _store;
    private readonly RefreshTokenOption _option;

    /// <summary>Initializes the service with its store and validated settings.</summary>
    /// <param name="store">The refresh-token persistence port.</param>
    /// <param name="option">The refresh-token generation settings.</param>
    /// <exception cref="ArgumentNullException">A dependency is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The settings are invalid.</exception>
    public RefreshTokenService(IRefreshTokenStore store, RefreshTokenOption option)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(option);

        Result<RefreshTokenOption> validation = TokenOptionValidator.Validate(option);
        if (validation.IsFailure)
        {
            throw new InvalidOperationException(
                string.Join("; ", validation.Errors.Select(error => error.Message)));
        }

        _store = store;
        _option = validation.Value;
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenIssue>> IssueAsync(
        string userId,
        DateTimeOffset nowUtc,
        Guid? tokenFamilyId = null,
        CancellationToken cancellationToken = default)
    {
        Result userValidation = TokenOptionValidator.ValidateUserId(userId);
        if (userValidation.IsFailure)
            return Result<RefreshTokenIssue>.Fail(userValidation.Errors);

        string plaintext = GenerateToken();
        DateTimeOffset createdAt = nowUtc.ToUniversalTime();
        Result<RefreshToken> created = RefreshToken.Create(
            Guid.NewGuid(),
            HashToken(plaintext),
            userId.Trim(),
            tokenFamilyId ?? Guid.NewGuid(),
            createdAt,
            createdAt.AddDays(_option.LifetimeDays));

        if (created.IsFailure)
            return Result<RefreshTokenIssue>.Fail(created.Errors);

        Result persisted = await _store
            .AddAsync(created.Value, cancellationToken)
            .ConfigureAwait(false);

        return persisted.IsSuccess
            ? Result<RefreshTokenIssue>.Ok(new RefreshTokenIssue(plaintext, created.Value))
            : Result<RefreshTokenIssue>.Fail(persisted.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<RefreshToken>> FindAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        Result tokenValidation = TokenOptionValidator.ValidateRefreshToken(token);
        if (tokenValidation.IsFailure)
            return Result<RefreshToken>.Fail(tokenValidation.Errors);

        Result<Maybe<RefreshToken>> found = await _store
            .FindByHashAsync(HashToken(token), cancellationToken)
            .ConfigureAwait(false);

        if (found.IsFailure)
            return Result<RefreshToken>.Fail(found.Errors);

        return found.Value.HasValue
            ? Result<RefreshToken>.Ok(found.Value.Value)
            : Result<RefreshToken>.Fail(RefreshTokenOutcome.Failure.NotFound);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeAsync(
        RefreshToken token,
        DateTimeOffset nowUtc,
        RefreshTokenRevocationReason reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        Result<RefreshToken> revoked = Result<RefreshToken>
            .Success(token)
            .Revoke(nowUtc, reason);

        return revoked.IsSuccess
            ? await _store.UpdateAsync(token, cancellationToken).ConfigureAwait(false)
            : Result.Fail(revoked.Errors);
    }

    /// <inheritdoc />
    public async Task<Result> MarkReuseAsync(
        RefreshToken token,
        DateTimeOffset detectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        Result<RefreshToken> marked = Result<RefreshToken>
            .Success(token)
            .DetectReuse(detectedAtUtc);

        return marked.IsSuccess
            ? await _store.UpdateAsync(token, cancellationToken).ConfigureAwait(false)
            : Result.Fail(marked.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenIssue>> RotateAsync(
        RefreshToken token,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        Result<RefreshToken> active = Result<RefreshToken>
            .Success(token)
            .EnsureActive(nowUtc);

        if (active.IsFailure)
            return Result<RefreshTokenIssue>.Fail(active.Errors);

        string plaintext = GenerateToken();
        DateTimeOffset rotatedAt = nowUtc.ToUniversalTime();
        Result<RefreshToken> replacement = RefreshToken.Create(
            Guid.NewGuid(),
            HashToken(plaintext),
            token.UserId,
            token.TokenFamilyId,
            rotatedAt,
            rotatedAt.AddDays(_option.LifetimeDays));

        if (replacement.IsFailure)
            return Result<RefreshTokenIssue>.Fail(replacement.Errors);

        Result<RefreshToken> replaced = Result<RefreshToken>
            .Success(token)
            .ReplaceWith(replacement.Value, rotatedAt);

        if (replaced.IsFailure)
            return Result<RefreshTokenIssue>.Fail(replaced.Errors);

        Result persisted = await _store
            .RotateAsync(token, replacement.Value, cancellationToken)
            .ConfigureAwait(false);

        return persisted.IsSuccess
            ? Result<RefreshTokenIssue>.Ok(new RefreshTokenIssue(plaintext, replacement.Value))
            : Result<RefreshTokenIssue>.Fail(persisted.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<int>> RevokeAllAsync(
        string userId,
        DateTimeOffset nowUtc,
        RefreshTokenRevocationReason reason = RefreshTokenRevocationReason.UserLogoutAll,
        CancellationToken cancellationToken = default)
    {
        Result userValidation = TokenOptionValidator.ValidateUserId(userId);
        if (userValidation.IsFailure)
            return Result<int>.Fail(userValidation.Errors);

        return await _store
            .RevokeAllAsync(userId.Trim(), nowUtc.ToUniversalTime(), reason, cancellationToken)
            .ConfigureAwait(false);
    }

    private string GenerateToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(_option.TokenByteLength);
        return string.Concat(_option.TokenPrefix, Convert.ToHexString(bytes));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
