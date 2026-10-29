using Core.Common.Monad.Results;

using Application.Common.Caching;

using Infrastructure.Caching.Services;

using Microsoft.Extensions.DependencyInjection;

using Testing;

namespace Infrastructure.Integration.Tests.Caching;

/// <summary>
/// Integration tests for <see cref="ICacheService"/> resolved from a real DI container
/// with HybridCache backed by a real Redis container.
/// </summary>
[Collection(RedisCachingCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class RedisCacheServiceIntegrationTest
{
    readonly ICacheService _cache;
    readonly RedisCachingFixture _fixture;

    public RedisCacheServiceIntegrationTest(RedisCachingFixture fixture)
    {
        _fixture = fixture;
        _cache = fixture.Cache;
    }

    [Fact(DisplayName = "Tests should skip when container runtime is unavailable")]
    public void Tests_ShouldSkipWhenContainerUnavailable()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }
    }

    [Fact(DisplayName = "ICacheService should resolve as CacheService with Redis backend")]
    public void ICacheService_ShouldResolveAsCacheService()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        var cache = _fixture.Services.GetRequiredService<ICacheService>();
        cache.Should().BeOfType<CacheService>();
    }

    [Fact(DisplayName = "GetOrCreateAsync should create once and serve the cached value via Redis")]
    public async Task GetOrCreateAsync_ShouldCreateOnceAndCacheViaRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        int calls = 0;
        string first = await _cache.GetValueAsync("redis:int:getorcreate:once", _ =>
        {
            calls++;
            return ValueTask.FromResult("created-via-redis");
        });
        string second = await _cache.GetValueAsync("redis:int:getorcreate:once", _ =>
        {
            calls++;
            return ValueTask.FromResult("should-not-run");
        });

        first.Should().Be("created-via-redis");
        second.Should().Be("created-via-redis");
        calls.Should().Be(1, "factory should only be called on the first miss");
    }

    [Fact(DisplayName = "SetAsync should store a value retrievable via Redis")]
    public async Task SetAsync_ShouldStoreValueRetrievableViaRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        await _cache.SetAsync("redis:int:set:get", "stored-in-redis");

        string value = await _cache.GetValueAsync(
            "redis:int:set:get",
            _ => ValueTask.FromResult("factory-should-not-run"));

        value.Should().Be("stored-in-redis");
    }

    [Fact(DisplayName = "SetAsync should overwrite an existing value in Redis")]
    public async Task SetAsync_ShouldOverwriteExistingValueInRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        await _cache.SetAsync("redis:int:set:overwrite", "original");
        await _cache.SetAsync("redis:int:set:overwrite", "updated");

        string value = await _cache.GetValueAsync(
            "redis:int:set:overwrite",
            _ => ValueTask.FromResult("factory-should-not-run"));

        value.Should().Be("updated");
    }

    [Fact(DisplayName = "RemoveAsync should evict the entry from Redis")]
    public async Task RemoveAsync_ShouldEvictEntryFromRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        await _cache.SetAsync("redis:int:remove:entry", "value");
        await _cache.RemoveAsync("redis:int:remove:entry");

        string value = await _cache.GetValueAsync(
            "redis:int:remove:entry",
            _ => ValueTask.FromResult("factory-after-remove"));

        value.Should().Be("factory-after-remove");
    }

    [Fact(DisplayName = "RemoveByTagAsync should evict entries with the tag via Redis")]
    public async Task RemoveByTagAsync_ShouldEvictMatchingTaggedEntriesViaRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        await _cache.SetAsync("redis:int:tag:evict:a", "one", tags: ["redis-evict-group"]);
        await _cache.SetAsync("redis:int:tag:evict:b", "two", tags: ["redis-evict-group"]);

        await _cache.RemoveByTagAsync("redis-evict-group");

        string a = await _cache.GetValueAsync(
            "redis:int:tag:evict:a",
            _ => ValueTask.FromResult("factory-a"));
        string b = await _cache.GetValueAsync(
            "redis:int:tag:evict:b",
            _ => ValueTask.FromResult("factory-b"));

        a.Should().Be("factory-a");
        b.Should().Be("factory-b");
    }

    [Fact(DisplayName = "GetOrCreateAsync should cache complex objects via Redis")]
    public async Task GetOrCreateAsync_ShouldCacheComplexObjectsViaRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        TestRecord? result = await _cache.GetValueAsync("redis:int:complex:obj", _ =>
            ValueTask.FromResult(new TestRecord { Id = 42, Name = "RedisUser" }));

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("RedisUser");
    }

    [Fact(DisplayName = "Multiple independent keys should not interfere with Redis")]
    public async Task MultipleKeys_ShouldNotInterfereWithRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        await _cache.SetAsync("redis:int:indep:a", "alpha");
        await _cache.SetAsync("redis:int:indep:b", "beta");
        await _cache.SetAsync("redis:int:indep:c", "gamma");

        string a = await _cache.GetValueAsync(
            "redis:int:indep:a",
            _ => ValueTask.FromResult("factory-a"));
        string b = await _cache.GetValueAsync(
            "redis:int:indep:b",
            _ => ValueTask.FromResult("factory-b"));
        string c = await _cache.GetValueAsync(
            "redis:int:indep:c",
            _ => ValueTask.FromResult("factory-c"));

        a.Should().Be("alpha");
        b.Should().Be("beta");
        c.Should().Be("gamma");
    }

    [Fact(DisplayName = "GetOrCreateAsync with custom options should cache via Redis")]
    public async Task GetOrCreateAsync_WithCustomOptions_ShouldCacheViaRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        var options = new CachingEntryOption
        {
            Expiration = TimeSpan.FromMinutes(10)
        };

        string value = await _cache.GetValueAsync(
            "redis:int:options:custom",
            _ => ValueTask.FromResult("redis-option-value"),
            options);

        value.Should().Be("redis-option-value");

        // Second call should return cached value without invoking factory
        string cached = await _cache.GetValueAsync(
            "redis:int:options:custom",
            _ => ValueTask.FromResult("should-not-run"));

        cached.Should().Be("redis-option-value");
    }

    [Fact(DisplayName = "GetOrCreateAsync with blank key should return KeyRequired with Redis backend")]
    public async Task GetOrCreateAsync_WithBlankKey_ShouldReturnKeyRequiredWithRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        Result<string> result = await _cache.GetOrCreateAsync(
            "  ",
            _ => ValueTask.FromResult("value"));

        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "SetAsync with null value should return ValueRequired with Redis backend")]
    public async Task SetAsync_WithNullValue_ShouldReturnValueRequiredWithRedis()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Assert.Skip(ContainerRuntime.SkipReason);
        }

        Result result = await _cache.SetAsync<string>("key", null!);

        result.Errors[0].Code.Should().Be("Caching.Value.Required");
    }

    /// <summary>Test record for complex object caching.</summary>
    sealed record TestRecord
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }
}
