using Core.BuildingBlocks.Domain.Entities;

namespace Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

/// <summary>Contract for a persisted refresh token.</summary>
public interface IRefreshToken : IEntity<Guid>
{
    /// <summary>Gets the hash of the refresh token secret.</summary>
    string TokenHash { get; }

    /// <summary>Gets the identifier of the user who owns the token.</summary>
    string UserId { get; }

    /// <summary>Gets the identifier shared by every token in the rotation family.</summary>
    Guid TokenFamilyId { get; }

    /// <summary>Gets the UTC moment the token was issued.</summary>
    DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets the UTC moment the token expires.</summary>
    DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets the UTC moment the token was revoked, when it has been revoked.</summary>
    DateTimeOffset? RevokedAtUtc { get; }

    /// <summary>Gets the reason the token was revoked, when it has been revoked.</summary>
    RefreshTokenRevocationReason? RevocationReason { get; }

    /// <summary>Gets the token that replaced this one during rotation, when rotation occurred.</summary>
    RefreshToken? ReplacedByToken { get; }

    /// <summary>Gets a value indicating whether the token has been revoked.</summary>
    bool IsRevoked { get; }

    /// <summary>Determines whether the token has expired at the supplied moment.</summary>
    /// <param name="nowUtc">The moment to compare against the expiry.</param>
    /// <returns><c>true</c> when the token is expired; otherwise <c>false</c>.</returns>
    bool IsExpired(DateTimeOffset nowUtc);

    /// <summary>Determines whether the token is neither revoked nor expired.</summary>
    /// <param name="nowUtc">The moment to compare against the expiry.</param>
    /// <returns><c>true</c> when the token is usable; otherwise <c>false</c>.</returns>
    bool IsActive(DateTimeOffset nowUtc);
}
