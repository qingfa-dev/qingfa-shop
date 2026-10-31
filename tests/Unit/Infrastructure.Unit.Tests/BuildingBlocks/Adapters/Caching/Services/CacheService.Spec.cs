using Application.BuildingBlocks.Caching;

using Infrastructure.BuildingBlocks.Adapters.Caching.Services;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Caching.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CacheServiceTest
{
    [Fact(DisplayName = "GetOrCreateAsync should create once and serve the stored value")]
    public async Task GetOrCreateAsync_ShouldCreateOnceAndServeTheStoredValue()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        int calls = 0;
        Result<string> first = await cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("value");
        });
        Result<string> second = await cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("other");
        });

        first.IsSuccess.Should().BeTrue();
        first.Value.Should().Be("value");
        second.Value.Should().Be("value");
        calls.Should().Be(1);
    }

    [Fact(DisplayName = "SetAsync should store a value that GetOrCreateAsync returns")]
    public async Task SetAsync_ShouldStoreAValueThatGetOrCreateAsyncReturns()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        (await cache.SetAsync("key", "stored")).IsSuccess.Should().BeTrue();

        Result<string> value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory"));

        value.Value.Should().Be("stored");
    }

    [Fact(DisplayName = "RemoveAsync should evict the entry")]
    public async Task RemoveAsync_ShouldEvictTheEntry()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        (await cache.SetAsync("key", "stored")).IsSuccess.Should().BeTrue();
        (await cache.RemoveAsync("key")).IsSuccess.Should().BeTrue();

        Result<string> value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory"));

        value.Value.Should().Be("factory");
    }

    [Fact(DisplayName = "RemoveByTagAsync should evict every tagged entry")]
    public async Task RemoveByTagAsync_ShouldEvictEveryTaggedEntry()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("first", "one", tags: ["group"]);
        await cache.SetAsync("second", "two", tags: ["group"]);

        Result evict = await cache.RemoveByTagAsync("group");

        evict.IsSuccess.Should().BeTrue();

        Result<string> first = await cache.GetOrCreateAsync(
            "first",
            _ => ValueTask.FromResult("factory-first"));
        Result<string> second = await cache.GetOrCreateAsync(
            "second",
            _ => ValueTask.FromResult("factory-second"));

        first.Value.Should().Be("factory-first");
        second.Value.Should().Be("factory-second");
    }

    [Fact(DisplayName = "GetOrCreateAsync with a blank key should report the key contract failure")]
    public async Task GetOrCreateAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result<string> result = await cache.GetOrCreateAsync(
            " ",
            _ => ValueTask.FromResult("value"));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "SetAsync with a null value should report the value contract failure")]
    public async Task SetAsync_WithNullValue_ShouldReturnValueRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result result = await cache.SetAsync<string>("key", null!);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Caching.Value.Required");
    }

    [Fact(DisplayName = "RemoveByTagAsync with no tags should succeed as a no-op")]
    public async Task RemoveByTagAsync_WithNoTags_ShouldSucceed()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result result = await cache.RemoveByTagAsync(Array.Empty<string>());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOrCreateAsync should report a provider failure as a read failure")]
    public async Task GetOrCreateAsync_WhenProviderThrows_ShouldReturnReadFailed()
    {
        ICacheService cache = new CacheService(
            new ThrowingHybridCache(),
            NullLogger<CacheService>.Instance);

        Result<string> result = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Caching.Read.ProviderUnavailable");
    }

    [Fact(DisplayName = "SetAsync should report a provider failure as a write failure")]
    public async Task SetAsync_WhenProviderThrows_ShouldReturnWriteFailed()
    {
        ICacheService cache = new CacheService(
            new ThrowingHybridCache(),
            NullLogger<CacheService>.Instance);

        Result result = await cache.SetAsync("key", "value");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Caching.Write.ProviderUnavailable");
    }

    [Fact(DisplayName = "RemoveAsync should report a provider failure as a remove failure")]
    public async Task RemoveAsync_WhenProviderThrows_ShouldReturnRemoveFailed()
    {
        ICacheService cache = new CacheService(
            new ThrowingHybridCache(),
            NullLogger<CacheService>.Instance);

        Result result = await cache.RemoveAsync("key");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Caching.Remove.ProviderUnavailable");
    }

    [Fact(DisplayName = "RemoveByTagAsync should report a provider failure as an eviction failure")]
    public async Task RemoveByTagAsync_WhenProviderThrows_ShouldReturnEvictFailed()
    {
        ICacheService cache = new CacheService(
            new ThrowingHybridCache(),
            NullLogger<CacheService>.Instance);

        Result single = await cache.RemoveByTagAsync("tag");
        Result collection = await cache.RemoveByTagAsync(["tag"]);

        single.Errors[0].Code.Should().Be("Caching.Evict.ProviderUnavailable");
        collection.Errors[0].Code.Should().Be("Caching.Evict.ProviderUnavailable");
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDistributedMemoryCache();
        services.AddMemoryCache();
        services.AddHybridCache();

        return services.BuildServiceProvider();
    }

    private static ICacheService CreateCache(IServiceProvider provider)
    {
        return new CacheService(
            provider.GetRequiredService<HybridCache>(),
            NullLogger<CacheService>.Instance);
    }

    private sealed class ThrowingHybridCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider unavailable");

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider unavailable");

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider unavailable");

        public override ValueTask RemoveByTagAsync(
            IEnumerable<string> tags,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider unavailable");

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider unavailable");
    }
}
