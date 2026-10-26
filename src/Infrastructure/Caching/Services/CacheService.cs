using Application.Common.Caching;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Caching.Services;

/// <summary>Wraps <see cref="HybridCache"/> with the application's cache contract and logging.</summary>
internal sealed partial class CacheService(
    HybridCache hybridCache,
    ILogger<CacheService> logger) : ICacheService
{
    /// <inheritdoc />
    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        CachingGuard.Key(key);
        CachingGuard.Factory(factory);

        bool created = false;

        // Call: HybridCache owns the L1/L2 lookup, stampede protection, and serialization.
        T result = await hybridCache.GetOrCreateAsync(
            key,
            async token =>
            {
                created = true;
                return await factory(token).ConfigureAwait(false);
            },
            options.ToHybridCacheEntryOptions(),
            tags,
            cancellationToken).ConfigureAwait(false);

        // Log: factory invocation is the only reliable miss signal; a hit never runs it.
        if (created)
        {
            Loggers.CacheMiss(logger, key);
        }
        else
        {
            Loggers.CacheHit(logger, key);
        }

        return result;
    }

    /// <inheritdoc />
    public async ValueTask SetAsync<T>(
        string key,
        T value,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        CachingGuard.Key(key);
        CachingGuard.Value(value);

        await hybridCache.SetAsync(
            key,
            value,
            options.ToHybridCacheEntryOptions(),
            tags,
            cancellationToken).ConfigureAwait(false);

        Loggers.CacheSet(logger, key);
    }

    /// <inheritdoc />
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        CachingGuard.Key(key);

        await hybridCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

        Loggers.CacheRemoved(logger, key);
    }

    /// <inheritdoc />
    public async ValueTask RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        string[] snapshot = [.. tags];
        if (snapshot.Length == 0)
        {
            return;
        }

        await hybridCache.RemoveByTagAsync(snapshot, cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            Loggers.CacheRemovedByTag(logger, string.Join(", ", snapshot));
        }
    }

    /// <inheritdoc />
    public async ValueTask RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default)
    {
        CachingGuard.Tag(tag);

        await hybridCache.RemoveByTagAsync([tag], cancellationToken).ConfigureAwait(false);

        Loggers.CacheRemovedByTag(logger, tag);
    }
}