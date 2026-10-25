using Application.Common.Caching;

using Infrastructure.Caching;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Caching;

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
        string first = await cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("value");
        });
        string second = await cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("other");
        });

        first.Should().Be("value");
        second.Should().Be("value");
        calls.Should().Be(1);
    }

    [Fact(DisplayName = "SetAsync should store a value that GetOrCreateAsync returns")]
    public async Task SetAsync_ShouldStoreAValueThatGetOrCreateAsyncReturns()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("key", "stored");

        string value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory"));

        value.Should().Be("stored");
    }

    [Fact(DisplayName = "RemoveAsync should evict the entry")]
    public async Task RemoveAsync_ShouldEvictTheEntry()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("key", "stored");
        await cache.RemoveAsync("key");

        string value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory"));

        value.Should().Be("factory");
    }

    [Fact(DisplayName = "RemoveByTagAsync should evict every tagged entry")]
    public async Task RemoveByTagAsync_ShouldEvictEveryTaggedEntry()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("first", "one", tags: ["group"]);
        await cache.SetAsync("second", "two", tags: ["group"]);

        await cache.RemoveByTagAsync("group");

        string first = await cache.GetOrCreateAsync(
            "first",
            _ => ValueTask.FromResult("factory-first"));
        string second = await cache.GetOrCreateAsync(
            "second",
            _ => ValueTask.FromResult("factory-second"));

        first.Should().Be("factory-first");
        second.Should().Be("factory-second");
    }

    [Fact(DisplayName = "GetOrCreateAsync with a blank key should report the contract error")]
    public async Task GetOrCreateAsync_WithBlankKey_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.GetOrCreateAsync(
            " ",
            _ => ValueTask.FromResult("value"));

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Key.Required*");
    }

    [Fact(DisplayName = "SetAsync with a null value should report the contract error")]
    public async Task SetAsync_WithNullValue_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.SetAsync<string>("key", null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("*Caching.Value.Required*");
    }

    [Fact(DisplayName = "RemoveByTagAsync with no tags should not throw")]
    public async Task RemoveByTagAsync_WithNoTags_ShouldNotThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.RemoveByTagAsync(Array.Empty<string>());

        await act.Should().NotThrowAsync();
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
}