using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;

using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Security.Authentication.Tokens.Stores;

/// <summary>Distributed-cache adapter for the access-token blacklist port.</summary>
public sealed class DistributedTokenBlacklistStore(
    IDistributedCache cache) : ITokenBlacklistStore
{
    /// <inheritdoc />
    public async Task<Result> SetAsync(
        string key,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        await cache.SetStringAsync(
            key,
            "1",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            },
            cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string? value = await cache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        return Result<bool>.Ok(value is not null);
    }
}
