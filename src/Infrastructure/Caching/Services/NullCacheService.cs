using Application.Common.Caching;

namespace Infrastructure.Caching.Services;

/// <summary>Cache facade used when caching is disabled: it always takes the factory path.</summary>
internal sealed class NullCacheService : ICacheService
{
    /// <inheritdoc />
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        CachingGuard.Key(key);
        CachingGuard.Factory(factory);

        return factory(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SetAsync<T>(
        string key,
        T value,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        CachingGuard.Key(key);
        CachingGuard.Value(value);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        CachingGuard.Key(key);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        CachingGuard.Tag(tag);

        return ValueTask.CompletedTask;
    }
}