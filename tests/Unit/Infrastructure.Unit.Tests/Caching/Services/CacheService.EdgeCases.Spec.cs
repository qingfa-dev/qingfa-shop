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

        Result<string> result = await cache.GetOrCreateAsync("key", token =>
        {
            token.CanBeCanceled.Should().BeTrue();
            return ValueTask.FromResult("value");
        }, cancellationToken: source.Token);

        result.Value.Should().Be("value");
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

        Result<string> result = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            options);

        result.Value.Should().Be("value");
    }

    [Fact(DisplayName = "GetOrCreateAsync with tags should store them")]
    public async Task GetOrCreateAsync_WithTags_ShouldStoreThem()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result<string> value = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            tags: ["tag1", "tag2"]);

        value.IsSuccess.Should().BeTrue();

        (await cache.RemoveByTagAsync("tag1")).IsSuccess.Should().BeTrue();

        Result<string> valueAfter = await cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("factory"));

        valueAfter.Value.Should().Be("factory");
    }

    [Fact(DisplayName = "SetAsync with custom options should succeed")]
    public async Task SetAsync_WithCustomOptions_ShouldSucceed()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10),
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            DisableDistributedCache = true
        };

        Result result = await cache.SetAsync("key", "value", options, ["tag1"]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOrCreateAsync with a null factory should report the factory contract failure")]
    public async Task GetOrCreateAsync_WithNullFactory_ShouldReturnFactoryRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result<string> result = await cache.GetOrCreateAsync<string>("key", null!);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Caching.Factory.Required");
    }

    [Fact(DisplayName = "RemoveByTagAsync with a null tag should report the tag contract failure")]
    public async Task RemoveByTagAsync_WithNullTag_ShouldReturnTagRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result result = await cache.RemoveByTagAsync((string)null!);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Caching.Tag.Required");
    }

    [Fact(DisplayName = "RemoveByTagAsync with a null collection should report the tags contract failure")]
    public async Task RemoveByTagAsync_WithNullCollection_ShouldReturnTagsRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result result = await cache.RemoveByTagAsync((IEnumerable<string>)null!);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Caching.Tags.Required");
    }

    [Fact(DisplayName = "RemoveByTagAsync with a blank member should report the tag contract failure")]
    public async Task RemoveByTagAsync_WithBlankMember_ShouldReturnTagRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result result = await cache.RemoveByTagAsync(["valid", " "]);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Caching.Tag.Required");
    }

    [Fact(DisplayName = "RemoveAsync with a blank key should report the key contract failure")]
    public async Task RemoveAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result result = await cache.RemoveAsync("  ");

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "GetOrCreateAsync should handle complex types")]
    public async Task GetOrCreateAsync_ShouldHandleComplexTypes()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        Result<TestObject> first = await cache.GetOrCreateAsync(
            "complex",
            _ => ValueTask.FromResult(new TestObject { Id = 1, Name = "test" }));

        Result<TestObject> second = await cache.GetOrCreateAsync(
            "complex",
            _ => ValueTask.FromResult(new TestObject { Id = 2, Name = "other" }));

        first.Value.Id.Should().Be(1);
        first.Value.Name.Should().Be("test");
        second.Value.Id.Should().Be(1);
        second.Value.Name.Should().Be("test");
    }

    [Fact(DisplayName = "SetAsync and GetOrCreateAsync should handle complex types")]
    public async Task SetAndGet_ShouldHandleComplexTypes()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("complex", new TestObject { Id = 42, Name = "stored" });

        Result<TestObject> value = await cache.GetOrCreateAsync(
            "complex",
            _ => ValueTask.FromResult(new TestObject { Id = 0, Name = "factory" }));

        value.Value.Id.Should().Be(42);
        value.Value.Name.Should().Be("stored");
    }

    [Fact(DisplayName = "Multiple keys should be independent")]
    public async Task MultipleKeys_ShouldBeIndependent()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("key1", "value1");
        await cache.SetAsync("key2", "value2");

        Result<string> value1 = await cache.GetOrCreateAsync(
            "key1",
            _ => ValueTask.FromResult("factory1"));
        Result<string> value2 = await cache.GetOrCreateAsync(
            "key2",
            _ => ValueTask.FromResult("factory2"));

        value1.Value.Should().Be("value1");
        value2.Value.Should().Be("value2");
    }

    [Fact(DisplayName = "RemoveAsync should not affect other keys")]
    public async Task RemoveAsync_ShouldNotAffectOtherKeys()
    {
        using ServiceProvider provider = BuildProvider();
        ICacheService cache = CreateCache(provider);

        await cache.SetAsync("key1", "value1");
        await cache.SetAsync("key2", "value2");
        await cache.RemoveAsync("key1");

        Result<string> value1 = await cache.GetOrCreateAsync(
            "key1",
            _ => ValueTask.FromResult("factory1"));
        Result<string> value2 = await cache.GetOrCreateAsync(
            "key2",
            _ => ValueTask.FromResult("factory2"));

        value1.Value.Should().Be("factory1");
        value2.Value.Should().Be("value2");
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
