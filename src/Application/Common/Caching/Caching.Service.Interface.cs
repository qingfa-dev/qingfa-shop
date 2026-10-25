namespace Application.Common.Caching;

/// <summary>Cache operations available to application use cases.</summary>
/// <remarks>
/// The port stays free of provider types so a handler can depend on it without
/// referencing Redis or HybridCache. The adapter lives in Infrastructure.
/// </remarks>
public interface ICacheService
{
    /// <summary>Returns a cached value, creating and storing it through <paramref name="factory"/> on a miss.</summary>
    /// <typeparam name="T">The cached value type. Null values are not cacheable.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="factory">The factory invoked on a miss.</param>
    /// <param name="options">Optional entry-specific overrides.</param>
    /// <param name="tags">Optional tags used for bulk invalidation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or the factory result when no entry exists.</returns>
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull;

    /// <summary>Stores a value in the cache.</summary>
    /// <typeparam name="T">The cached value type. Null values are not cacheable.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Optional entry-specific overrides.</param>
    /// <param name="tags">Optional tags used for bulk invalidation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SetAsync<T>(
        string key,
        T value,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull;

    /// <summary>Removes a single entry by key.</summary>
    /// <param name="key">The key to evict.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes every entry that carries any of the supplied tags.</summary>
    /// <param name="tags">The tags to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>Removes every entry that carries the supplied tag.</summary>
    /// <param name="tag">The tag to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}