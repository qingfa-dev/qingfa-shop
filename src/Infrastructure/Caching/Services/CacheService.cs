using Application.Common.Caching;

using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Caching.Services;

/// <summary>Wraps <see cref="HybridCache"/> with the application's monadic cache contract and logging.</summary>
internal sealed partial class CacheService(
    HybridCache hybridCache,
    ILogger<CacheService> logger) : ICacheService
{
    /// <inheritdoc />
    public async ValueTask<Result<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        string[]? tagSnapshot = tags?.ToArray();
        Error? validation =
            CachingGuard.Key(key) ??
            CachingGuard.Factory(factory) ??
            CachingGuard.Options(options) ??
            CachingGuard.Tags(tagSnapshot);

        if (validation is not null)
            return Result<T>.Fail(validation);

        bool created = false;

        try
        {
            // Call: HybridCache owns the L1/L2 lookup, stampede protection, and serialization.
            T result = await hybridCache.GetOrCreateAsync(
                key,
                async token =>
                {
                    created = true;
                    return await factory(token).ConfigureAwait(false);
                },
                options.ToHybridCacheEntryOptions(),
                tagSnapshot,
                cancellationToken).ConfigureAwait(false);

            // Log: factory invocation is the only reliable miss signal; a hit never runs it.
            if (created)
                Loggers.CacheMiss(logger, key);
            else
                Loggers.CacheHit(logger, key);

            return Result<T>.Ok(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Catch: report provider or factory failures as a cache availability failure.
            Loggers.CacheReadFailed(logger, key, exception);
            return Result<T>.Fail(CachingOutcome.Failure.ReadFailed(key, exception));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result> SetAsync<T>(
        string key,
        T value,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        string[]? tagSnapshot = tags?.ToArray();
        Error? validation =
            CachingGuard.Key(key) ??
            CachingGuard.Value(value) ??
            CachingGuard.Options(options) ??
            CachingGuard.Tags(tagSnapshot);

        if (validation is not null)
            return Result.Fail(validation);

        try
        {
            await hybridCache.SetAsync(
                key,
                value,
                options.ToHybridCacheEntryOptions(),
                tagSnapshot,
                cancellationToken).ConfigureAwait(false);

            Loggers.CacheSet(logger, key);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Loggers.CacheWriteFailed(logger, key, exception);
            return Result.Fail(CachingOutcome.Failure.WriteFailed(key, exception));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result> RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        Error? validation = CachingGuard.Key(key);
        if (validation is not null)
            return Result.Fail(validation);

        try
        {
            await hybridCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

            Loggers.CacheRemoved(logger, key);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Loggers.CacheRemoveFailed(logger, key, exception);
            return Result.Fail(CachingOutcome.Failure.RemoveFailed(key, exception));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result> RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        string[]? snapshot = tags?.ToArray();
        Error? validation = CachingGuard.TagsRequired(snapshot);
        if (validation is not null)
            return Result.Fail(validation);

        if (snapshot!.Length == 0)
            return Result.Ok();

        try
        {
            await hybridCache.RemoveByTagAsync(snapshot, cancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Debug))
                Loggers.CacheRemovedByTag(logger, string.Join(", ", snapshot));

            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Loggers.CacheEvictFailed(logger, string.Join(", ", snapshot), exception);
            return Result.Fail(CachingOutcome.Failure.EvictFailed(tag: null, exception));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result> RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default)
    {
        Error? validation = CachingGuard.Tag(tag);
        if (validation is not null)
            return Result.Fail(validation);

        try
        {
            await hybridCache.RemoveByTagAsync([tag], cancellationToken).ConfigureAwait(false);

            Loggers.CacheRemovedByTag(logger, tag);
            return Result.Ok();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Loggers.CacheEvictFailed(logger, tag, exception);
            return Result.Fail(CachingOutcome.Failure.EvictFailed(tag, exception));
        }
    }
}
