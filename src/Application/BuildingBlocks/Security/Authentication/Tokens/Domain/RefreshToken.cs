using Core.BuildingBlocks.Domain.Entities;

namespace Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

/// <summary>
/// A persisted refresh token with immutable identity and explicit revocation state.
/// </summary>
/// <remarks>
/// The token secret is never stored; only its hash is persisted. Creation normalizes
/// timestamps to UTC and validates every invariant before returning a usable entity.
/// </remarks>
public sealed class RefreshToken : Entity<Guid>, IRefreshToken
{
    /// <summary>Initializes an empty token so a persistence layer can materialize one.</summary>
    private RefreshToken()
    {
        TokenHash = string.Empty;
        UserId = string.Empty;
    }

    /// <summary>Initializes a token with its normalized domain state.</summary>
    private RefreshToken(
        Guid id,
        string tokenHash,
        string userId,
        Guid tokenFamilyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        TokenHash = tokenHash;
        UserId = userId;
        TokenFamilyId = tokenFamilyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <inheritdoc />
    public string TokenHash { get; private set; }

    /// <inheritdoc />
    public string UserId { get; private set; }

    /// <inheritdoc />
    public Guid TokenFamilyId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <inheritdoc />
    public RefreshTokenRevocationReason? RevocationReason { get; private set; }

    /// <inheritdoc />
    public RefreshToken? ReplacedByToken { get; private set; }

    /// <inheritdoc />
    public bool IsRevoked => RevokedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsExpired(DateTimeOffset nowUtc) =>
        nowUtc.ToUniversalTime() >= ExpiresAtUtc;

    /// <inheritdoc />
    public bool IsActive(DateTimeOffset nowUtc) =>
        !IsRevoked && !IsExpired(nowUtc);

    /// <summary>Creates a refresh token and validates every invariant.</summary>
    /// <param name="id">The token identifier.</param>
    /// <param name="tokenHash">The hash of the token secret; surrounding whitespace is removed.</param>
    /// <param name="userId">The owning user identifier; surrounding whitespace is removed.</param>
    /// <param name="tokenFamilyId">The identifier shared by the rotation family.</param>
    /// <param name="createdAtUtc">The moment the token was issued; converted to UTC.</param>
    /// <param name="expiresAtUtc">The moment the token expires; converted to UTC.</param>
    /// <returns>The created token, or the first broken rule.</returns>
    public static Result<RefreshToken> Create(
        Guid id,
        string? tokenHash,
        string? userId,
        Guid tokenFamilyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var candidate = new RefreshToken(
            id,
            tokenHash?.Trim() ?? string.Empty,
            userId?.Trim() ?? string.Empty,
            tokenFamilyId,
            createdAtUtc.ToUniversalTime(),
            expiresAtUtc.ToUniversalTime());

        return RefreshTokenValidator.Validate(candidate);
    }

    /// <summary>Records a revocation on the token; callers must validate before invoking this.</summary>
    internal void ApplyRevocation(
        DateTimeOffset revokedAtUtc,
        RefreshTokenRevocationReason reason)
    {
        RevokedAtUtc = revokedAtUtc.ToUniversalTime();
        RevocationReason = reason;
    }

    /// <summary>Records the token that replaced this one during rotation.</summary>
    internal void ApplyReplacement(RefreshToken replacement) =>
        ReplacedByToken = replacement;
}
