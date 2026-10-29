namespace Application.Common.Security.Authentication.Tokens.Services;

/// <summary>Blacklists access-token identifiers until their original expiry.</summary>
public interface ITokenBlacklist
{
    /// <summary>Adds a token identifier to the blacklist.</summary>
    /// <param name="jti">The access-token identifier.</param>
    /// <param name="expiresAtUtc">The access-token expiry; entries never outlive it.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the entry is stored or already unnecessary; otherwise a failure.</returns>
    Task<Result> AddAsync(
        string jti,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether a token identifier is blacklisted.</summary>
    /// <param name="jti">The access-token identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when blacklisted; otherwise <c>false</c>.</returns>
    Task<Result<bool>> ContainsAsync(
        string jti,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for the access-token blacklist.</summary>
public interface ITokenBlacklistStore
{
    /// <summary>Stores a blacklist entry with a bounded lifetime.</summary>
    /// <param name="key">The complete blacklist key.</param>
    /// <param name="lifetime">The remaining lifetime of the blacklist entry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when stored; otherwise a failure.</returns>
    Task<Result> SetAsync(
        string key,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether a blacklist entry exists.</summary>
    /// <param name="key">The complete blacklist key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when present; otherwise <c>false</c>.</returns>
    Task<Result<bool>> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);
}
