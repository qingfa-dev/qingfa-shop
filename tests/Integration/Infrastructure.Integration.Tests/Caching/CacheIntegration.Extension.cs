using Core.Common.Monad.Results;

using Application.Common.Caching;

namespace Infrastructure.Integration.Tests.Caching;

/// <summary>Small helpers that keep integration tests focused on behavior instead of result plumbing.</summary>
internal static class CacheIntegrationExtensions
{
    /// <summary>Reads a cached value and fails the test with the cache errors when the read fails.</summary>
    public static async Task<T> GetValueAsync<T>(
        this ICacheService cache,
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CachingEntryOption? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Result<T> result = await cache.GetOrCreateAsync(
            key,
            factory,
            options,
            tags,
            cancellationToken);

        result.IsSuccess.Should().BeTrue(
            $"cache read for '{key}' should succeed but returned: {string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Message}"))}");

        return result.Value;
    }
}
