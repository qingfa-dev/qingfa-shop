using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Common.Security.Authentication.Tokens.Options;

namespace Infrastructure.Security.Authentication.Tokens.Services;

/// <summary>
/// Blacklists access-token identifiers for at most the remaining lifetime of the token.
/// </summary>
public sealed class TokenBlacklistService : ITokenBlacklist
{
    private readonly ITokenBlacklistStore _store;
    private readonly TokenBlacklistOption _option;
    private readonly TimeProvider _clock;

    /// <summary>Initializes the service with its store, settings, and clock.</summary>
    /// <param name="store">The blacklist persistence port.</param>
    /// <param name="option">The blacklist settings.</param>
    /// <param name="clock">The clock used to bound entry lifetimes.</param>
    /// <exception cref="ArgumentNullException">A dependency is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The settings are invalid.</exception>
    public TokenBlacklistService(
        ITokenBlacklistStore store,
        TokenBlacklistOption option,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(clock);

        Result<TokenBlacklistOption> validation = TokenOptionValidator.Validate(option);
        if (validation.IsFailure)
        {
            throw new InvalidOperationException(
                string.Join("; ", validation.Errors.Select(error => error.Message)));
        }

        _store = store;
        _option = validation.Value;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync(
        string jti,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        Result jtiValidation = TokenOptionValidator.ValidateJti(jti);
        if (jtiValidation.IsFailure)
            return jtiValidation;

        if (!_option.Enabled)
            return Result.Ok();

        if (expiresAtUtc == default)
            return Result.Fail(TokenOutcome.Failure.BlacklistExpiryRequired);

        // Compute: An expired access token is already unusable, so it needs no entry.
        TimeSpan lifetime = expiresAtUtc.ToUniversalTime() - _clock.GetUtcNow();
        if (lifetime <= TimeSpan.Zero)
            return Result.Ok();

        return await _store
            .SetAsync(Key(jti), lifetime, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ContainsAsync(
        string jti,
        CancellationToken cancellationToken = default)
    {
        Result jtiValidation = TokenOptionValidator.ValidateJti(jti);
        if (jtiValidation.IsFailure)
            return Result<bool>.Fail(jtiValidation.Errors);

        if (!_option.Enabled)
            return Result<bool>.Ok(false);

        return await _store
            .ExistsAsync(Key(jti), cancellationToken)
            .ConfigureAwait(false);
    }

    private string Key(string jti) => string.Concat(_option.KeyPrefix, jti);
}
