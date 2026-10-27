using Application.Common.Caching;

using Infrastructure.Caching.Services;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Unit.Tests.Caching.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CacheServiceEdgeCasesTest
{
    [Fact(DisplayName = "GetOrCreateAsync should pass cancellation token to factory")]
    public async Task GetOrCreateAsync_ShouldPassCancellationTokenToFactory()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);
        CancellationTokenSource source = new();

        await cache.GetOrCreateAsync("key", token =>
        {
            token.CanBeCanceled.Should().BeTrue();
            return ValueTask.FromResult("value");
        }, cancellationToken: source.Token);
    }

    [Fact(DisplayName = "GetOrCreateAsync with custom options should pass them through")]
    public async Task GetOrCreateAsync_WithCustomOptions_ShouldPassThemThrough()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10),
            DisableLocalCache = true
        };

        string value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            options);

        value.Should().Be("value");
    }

    [Fact(DisplayName = "GetOrCreateAsync with tags should store them")]
    public async Task GetOrCreateAsync_WithTags_ShouldStoreThem()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        string value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            tags: new[] { "tag1", "tag2" });

        value.Should().Be("value");

        await cache.RemoveByTagAsync("tag1");

        string valueAfter = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory"));

        valueAfter.Should().Be("factory");
    }

    [Fact(DisplayName = "SetAsync with custom options should not throw")]
    public async Task SetAsync_WithCustomOptions_ShouldNotThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            DisableDistributedCache = true
        };

        Func<Task> act = async () => await cache.SetAsync(
            "key",
            "value",
            options,
            new[] { "tag1" });

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "SetAsync with null value should throw")]
    public async Task SetAsync_WithNullValue_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.SetAsync<string>("key", null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("*Caching.Value.Required*");
    }

    [Fact(DisplayName = "GetOrCreateAsync with null factory should throw")]
    public async Task GetOrCreateAsync_WithNullFactory_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.GetOrCreateAsync<string>(
            "key",
            null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("*Caching.Factory.Required*");
    }

    [Fact(DisplayName = "RemoveByTagAsync with collection should evict tagged entries")]
    public async Task RemoveByTagAsync_WithCollection_ShouldEvictTaggedEntries()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("first", "one", tags: new[] { "group" });
        await cache.SetAsync("second", "two", tags: new[] { "group" });
        await cache.SetAsync("third", "three", tags: new[] { "other" });

        await cache.RemoveByTagAsync(new[] { "group" });

        string first = await cache.GetOrCreateAsync(
            "first",
            _ => ValueTask.FromResult("factory-first"));
        string second = await cache.GetOrCreateAsync(
            "second",
            _ => ValueTask.FromResult("factory-second"));

        first.Should().Be("factory-first");
        second.Should().Be("factory-second");
    }

    [Fact(DisplayName = "RemoveByTagAsync with null tag should throw")]
    public async Task RemoveByTagAsync_WithNullTag_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.RemoveByTagAsync((string)null!);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Tag.Required*");
    }

    [Fact(DisplayName = "RemoveByTagAsync with null collection should throw")]
    public async Task RemoveByTagAsync_WithNullCollection_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.RemoveByTagAsync((IEnumerable<string>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "RemoveAsync with blank key should throw")]
    public async Task RemoveAsync_WithBlankKey_ShouldThrow()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Func<Task> act = async () => await cache.RemoveAsync("  ");

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Key.Required*");
    }

    [Fact(DisplayName = "GetOrCreateAsync should handle complex types")]
    public async Task GetOrCreateAsync_ShouldHandleComplexTypes()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        TestObject first = await cache.GetOrCreateAsync(
            "complex",
            _ => ValueTask.FromResult(new TestObject { Id = 1, Name = "test" }));

        TestObject second = await cache.GetOrCreateAsync(
            "complex",
            _ => ValueTask.FromResult(new TestObject { Id = 2, Name = "other" }));

        first.Id.Should().Be(1);
        first.Name.Should().Be("test");
        second.Id.Should().Be(1);
        second.Name.Should().Be("test");
    }

    [Fact(DisplayName = "SetAsync and GetOrCreateAsync should handle complex types")]
    public async Task SetAndGet_ShouldHandleComplexTypes()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("complex", new TestObject { Id = 42, Name = "stored" });

        TestObject value = await cache.GetOrCreateAsync(
            "complex",
            _ => ValueTask.FromResult(new TestObject { Id = 0, Name = "factory" }));

        value.Id.Should().Be(42);
        value.Name.Should().Be("stored");
    }

    [Fact(DisplayName = "Multiple keys should be independent")]
    public async Task MultipleKeys_ShouldBeIndependent()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("key1", "value1");
        await cache.SetAsync("key2", "value2");

        string value1 = await cache.GetOrCreateAsync(
            "key1",
            _ => ValueTask.FromResult("factory1"));
        string value2 = await cache.GetOrCreateAsync(
            "key2",
            _ => ValueTask.FromResult("factory2"));

        value1.Should().Be("value1");
        value2.Should().Be("value2");
    }

    [Fact(DisplayName = "RemoveAsync should not affect other keys")]
    public async Task RemoveAsync_ShouldNotAffectOtherKeys()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("key1", "value1");
        await cache.SetAsync("key2", "value2");
        await cache.RemoveAsync("key1");

        string value1 = await cache.GetOrCreateAsync(
            "key1",
            _ => ValueTask.FromResult("factory1"));
        string value2 = await cache.GetOrCreateAsync(
            "key2",
            _ => ValueTask.FromResult("factory2"));

        value1.Should().Be("factory1");
        value2.Should().Be("value2");
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

    private sealed class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
