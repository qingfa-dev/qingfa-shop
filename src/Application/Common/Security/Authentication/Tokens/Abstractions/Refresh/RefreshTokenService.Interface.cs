using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Common.Security.Authentication.Tokens.Abstractions.Refresh;

/// <summary>Issues, finds, revokes, and rotates persisted refresh tokens.</summary>
public interface IRefreshTokenService
{
    /// <summary>Issues a new opaque refresh token and persists its hash.</summary>
    /// <param name="userId">The user who owns the token.</param>
    /// <param name="nowUtc">The UTC issue moment.</param>
    /// <param name="tokenFamilyId">The existing rotation family, or <c>null</c> for a new family.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plaintext token and its persisted entity, or a failure.</returns>
    Task<Result<RefreshTokenIssue>> IssueAsync(
        string userId,
        DateTimeOffset nowUtc,
        Guid? tokenFamilyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Finds a refresh token by its plaintext value.</summary>
    /// <param name="token">The plaintext token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted entity, or a failure when it is unknown or malformed.</returns>
    Task<Result<RefreshToken>> FindAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token.</summary>
    /// <param name="token">The token to revoke.</param>
    /// <param name="nowUtc">The UTC revocation moment.</param>
    /// <param name="reason">The revocation reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the revocation is persisted; otherwise a failure.</returns>
    Task<Result> RevokeAsync(
        RefreshToken token,
        DateTimeOffset nowUtc,
        RefreshTokenRevocationReason reason,
        CancellationToken cancellationToken = default);

    /// <summary>Records refresh-token reuse without changing an existing stronger reason.</summary>
    /// <param name="token">The reused token.</param>
    /// <param name="detectedAtUtc">The UTC moment reuse was detected.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the theft marker is persisted; otherwise a failure.</returns>
    Task<Result> MarkReuseAsync(
        RefreshToken token,
        DateTimeOffset detectedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Rotates an active token into a fresh token in the same family.</summary>
    /// <param name="token">The current token.</param>
    /// <param name="nowUtc">The UTC rotation moment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The replacement token, or a failure.</returns>
    Task<Result<RefreshTokenIssue>> RotateAsync(
        RefreshToken token,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes every refresh token owned by a user.</summary>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="nowUtc">The UTC revocation moment.</param>
    /// <param name="reason">The revocation reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of tokens revoked, or a failure.</returns>
    Task<Result<int>> RevokeAllAsync(
        string userId,
        DateTimeOffset nowUtc,
        RefreshTokenRevocationReason reason = RefreshTokenRevocationReason.UserLogoutAll,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for refresh tokens.</summary>
public interface IRefreshTokenStore
{
    /// <summary>Finds a token by its hash.</summary>
    /// <param name="tokenHash">The stored token hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching token when present.</returns>
    Task<Result<Maybe<RefreshToken>>> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a newly issued token.</summary>
    /// <param name="token">The token to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the token is stored; otherwise a failure.</returns>
    Task<Result> AddAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing token.</summary>
    /// <param name="token">The token to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the update is stored; otherwise a failure.</returns>
    Task<Result> UpdateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);

    /// <summary>Atomically stores a replacement and revokes the current token.</summary>
    /// <param name="current">The current token, already marked as replaced.</param>
    /// <param name="replacement">The newly issued replacement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when both changes are stored; otherwise a failure.</returns>
    Task<Result> RotateAsync(
        RefreshToken current,
        RefreshToken replacement,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes every token owned by a user.</summary>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="revokedAtUtc">The UTC revocation moment.</param>
    /// <param name="reason">The revocation reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of tokens revoked, or a failure.</returns>
    Task<Result<int>> RevokeAllAsync(
        string userId,
        DateTimeOffset revokedAtUtc,
        RefreshTokenRevocationReason reason,
        CancellationToken cancellationToken = default);
}
