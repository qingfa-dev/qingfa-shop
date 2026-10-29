using Core.Common.Monad.Results;

using Application.Common.Caching;

namespace Infrastructure.Integration.Tests.Caching;

/// <summary>
/// Integration tests for <see cref="ICacheService"/> resolved from a real DI container
/// with HybridCache, MemoryCache, and DistributedMemoryCache.
/// </summary>
[Collection(CachingCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class CacheServiceIntegrationTest
{
    readonly ICacheService _cache;

    public CacheServiceIntegrationTest(CachingFixture fixture)
    {
        _cache = fixture.Cache;
    }

    #region GetOrCreateAsync

    [Fact(DisplayName = "GetOrCreateAsync should create once and serve the cached value")]
    public async Task GetOrCreateAsync_ShouldCreateOnceAndCache()
    {
        int calls = 0;
        string first = await _cache.GetValueAsync("int:getorcreate:once", _ =>
        {
            calls++;
            return ValueTask.FromResult("created");
        });
        string second = await _cache.GetValueAsync("int:getorcreate:once", _ =>
        {
            calls++;
            return ValueTask.FromResult("should-not-run");
        });

        first.Should().Be("created");
        second.Should().Be("created");
        calls.Should().Be(1, "factory should only be called on the first miss");
    }

    [Fact(DisplayName = "GetOrCreateAsync should cache different types independently")]
    public async Task GetOrCreateAsync_ShouldCacheDifferentTypesIndependently()
    {
        int stringCalls = 0;
        int intCalls = 0;

        string str = await _cache.GetValueAsync("int:types:string", _ =>
        {
            stringCalls++;
            return ValueTask.FromResult("hello");
        });
        int num = await _cache.GetValueAsync("int:types:int", _ =>
        {
            intCalls++;
            return ValueTask.FromResult(42);
        });

        str.Should().Be("hello");
        num.Should().Be(42);
        stringCalls.Should().Be(1);
        intCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetOrCreateAsync should cache complex objects")]
    public async Task GetOrCreateAsync_ShouldCacheComplexObjects()
    {
        TestRecord? result = await _cache.GetValueAsync("int:complex:obj", _ =>
            ValueTask.FromResult(new TestRecord { Id = 1, Name = "Alice" }));

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Alice");
    }

    [Fact(DisplayName = "GetOrCreateAsync with blank key should return KeyRequired")]
    public async Task GetOrCreateAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        Result<string> result = await _cache.GetOrCreateAsync(
            "  ",
            _ => ValueTask.FromResult("value"));

        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "GetOrCreateAsync with null factory should return FactoryRequired")]
    public async Task GetOrCreateAsync_WithNullFactory_ShouldReturnFactoryRequired()
    {
        Result<string> result = await _cache.GetOrCreateAsync(
            "key",
            (Func<CancellationToken, ValueTask<string>>)null!);

        result.Errors[0].Code.Should().Be("Caching.Factory.Required");
    }

    [Fact(DisplayName = "GetOrCreateAsync should pass a cancellable token to factory")]
    public async Task GetOrCreateAsync_ShouldPassCancellationToken()
    {
        CancellationToken? receivedToken = null;

        var cts = new CancellationTokenSource();
        await _cache.GetValueAsync(
            "int:ct:test",
            token =>
            {
                receivedToken = token;
                return ValueTask.FromResult("ok");
            },
            cancellationToken: cts.Token);

        receivedToken.Should().NotBeNull();
        receivedToken!.Value.CanBeCanceled.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOrCreateAsync with custom options should cache the value")]
    public async Task GetOrCreateAsync_WithCustomOptions_ShouldCache()
    {
        var options = new CachingEntryOption
        {
            Expiration = TimeSpan.FromMinutes(10)
        };

        string value = await _cache.GetValueAsync(
            "int:options:custom",
            _ => ValueTask.FromResult("option-value"),
            options);

        value.Should().Be("option-value");

        // Second call should return cached value without invoking factory
        string cached = await _cache.GetValueAsync(
            "int:options:custom",
            _ => ValueTask.FromResult("should-not-run"));

        cached.Should().Be("option-value");
    }

    [Fact(DisplayName = "GetOrCreateAsync with tags should cache the value")]
    public async Task GetOrCreateAsync_WithTags_ShouldCache()
    {
        string value = await _cache.GetValueAsync(
            "int:tags:create",
            _ => ValueTask.FromResult("tagged-value"),
            tags: ["integration-tag"]);

        value.Should().Be("tagged-value");
    }

    #endregion

    #region SetAsync

    [Fact(DisplayName = "SetAsync should store a value retrievable by GetOrCreateAsync")]
    public async Task SetAsync_ShouldStoreValueRetrievableByGetOrCreate()
    {
        await _cache.SetAsync("int:set:get", "stored-value");

        string value = await _cache.GetValueAsync(
            "int:set:get",
            _ => ValueTask.FromResult("factory-should-not-run"));

        value.Should().Be("stored-value");
    }

    [Fact(DisplayName = "SetAsync should overwrite an existing value")]
    public async Task SetAsync_ShouldOverwriteExistingValue()
    {
        await _cache.SetAsync("int:set:overwrite", "original");
        await _cache.SetAsync("int:set:overwrite", "updated");

        string value = await _cache.GetValueAsync(
            "int:set:overwrite",
            _ => ValueTask.FromResult("factory-should-not-run"));

        value.Should().Be("updated");
    }

    [Fact(DisplayName = "SetAsync with tags should store the value")]
    public async Task SetAsync_WithTags_ShouldStoreValue()
    {
        await _cache.SetAsync("int:set:tags", "tagged", tags: ["group-a", "group-b"]);

        string value = await _cache.GetValueAsync(
            "int:set:tags",
            _ => ValueTask.FromResult("factory-should-not-run"));

        value.Should().Be("tagged");
    }

    [Fact(DisplayName = "SetAsync with blank key should return KeyRequired")]
    public async Task SetAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        Result result = await _cache.SetAsync("  ", "value");

        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "SetAsync with null value should return ValueRequired")]
    public async Task SetAsync_WithNullValue_ShouldReturnValueRequired()
    {
        Result result = await _cache.SetAsync<string>("key", null!);

        result.Errors[0].Code.Should().Be("Caching.Value.Required");
    }

    #endregion

    #region RemoveAsync

    [Fact(DisplayName = "RemoveAsync should evict the entry")]
    public async Task RemoveAsync_ShouldEvictEntry()
    {
        await _cache.SetAsync("int:remove:entry", "value");
        await _cache.RemoveAsync("int:remove:entry");

        string value = await _cache.GetValueAsync(
            "int:remove:entry",
            _ => ValueTask.FromResult("factory-after-remove"));

        value.Should().Be("factory-after-remove");
    }

    [Fact(DisplayName = "RemoveAsync on non-existent key should not throw")]
    public async Task RemoveAsync_NonExistentKey_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.RemoveAsync("int:remove:nonexistent");

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "RemoveAsync with blank key should return KeyRequired")]
    public async Task RemoveAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        Result result = await _cache.RemoveAsync("");

        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    #endregion

    #region RemoveByTagAsync

    [Fact(DisplayName = "RemoveByTagAsync should evict entries with the matching tag")]
    public async Task RemoveByTagAsync_ShouldEvictMatchingTaggedEntries()
    {
        await _cache.SetAsync("int:tag:evict:a", "one", tags: ["evict-group"]);
        await _cache.SetAsync("int:tag:evict:b", "two", tags: ["evict-group"]);

        await _cache.RemoveByTagAsync("evict-group");

        string a = await _cache.GetValueAsync(
            "int:tag:evict:a",
            _ => ValueTask.FromResult("factory-a"));
        string b = await _cache.GetValueAsync(
            "int:tag:evict:b",
            _ => ValueTask.FromResult("factory-b"));

        a.Should().Be("factory-a");
        b.Should().Be("factory-b");
    }

    [Fact(DisplayName = "RemoveByTagAsync with collection should evict matching entries")]
    public async Task RemoveByTagAsync_WithCollection_ShouldEvictMatchingEntries()
    {
        await _cache.SetAsync("int:tag:multi:a", "one", tags: ["multi-a"]);
        await _cache.SetAsync("int:tag:multi:b", "two", tags: ["multi-b"]);

        await _cache.RemoveByTagAsync(new[] { "multi-a", "multi-b" });

        string a = await _cache.GetValueAsync(
            "int:tag:multi:a",
            _ => ValueTask.FromResult("factory-a"));
        string b = await _cache.GetValueAsync(
            "int:tag:multi:b",
            _ => ValueTask.FromResult("factory-b"));

        a.Should().Be("factory-a");
        b.Should().Be("factory-b");
    }

    [Fact(DisplayName = "RemoveByTagAsync with empty collection should not throw")]
    public async Task RemoveByTagAsync_EmptyCollection_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.RemoveByTagAsync(Array.Empty<string>());

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "RemoveByTagAsync with blank tag should return TagRequired")]
    public async Task RemoveByTagAsync_WithBlankTag_ShouldReturnTagRequired()
    {
        Result result = await _cache.RemoveByTagAsync("  ");

        result.Errors[0].Code.Should().Be("Caching.Tag.Required");
    }

    #endregion

    #region Cancellation

    [Fact(DisplayName = "SetAsync should respect cancellation token")]
    public async Task SetAsync_ShouldRespectCancellationToken()
    {
        var cts = new CancellationTokenSource();
        Func<Task> act = async () => await _cache.SetAsync(
            "int:cancel:set",
            "value",
            cancellationToken: cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "RemoveAsync should respect cancellation token")]
    public async Task RemoveAsync_ShouldRespectCancellationToken()
    {
        var cts = new CancellationTokenSource();
        Func<Task> act = async () => await _cache.RemoveAsync(
            "int:cancel:remove",
            cts.Token);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Independent keys

    [Fact(DisplayName = "Multiple independent keys should not interfere")]
    public async Task MultipleKeys_ShouldNotInterfere()
    {
        await _cache.SetAsync("int:indep:a", "alpha");
        await _cache.SetAsync("int:indep:b", "beta");
        await _cache.SetAsync("int:indep:c", "gamma");

        string a = await _cache.GetValueAsync(
            "int:indep:a",
            _ => ValueTask.FromResult("factory-a"));
        string b = await _cache.GetValueAsync(
            "int:indep:b",
            _ => ValueTask.FromResult("factory-b"));
        string c = await _cache.GetValueAsync(
            "int:indep:c",
            _ => ValueTask.FromResult("factory-c"));

        a.Should().Be("alpha");
        b.Should().Be("beta");
        c.Should().Be("gamma");
    }

    #endregion

    /// <summary>Test record for complex object caching.</summary>
    sealed record TestRecord
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }
}
