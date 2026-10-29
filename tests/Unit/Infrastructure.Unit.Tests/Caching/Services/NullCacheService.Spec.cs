using Application.Common.Caching;

using Infrastructure.Caching.Services;

namespace Infrastructure.Unit.Tests.Caching.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Caching")]
public sealed class NullCacheServiceTest
{
    private readonly NullCacheService _cache = new();

    [Fact(DisplayName = "GetOrCreateAsync should always invoke the factory")]
    public async Task GetOrCreateAsync_ShouldAlwaysInvokeFactory()
    {
        int calls = 0;
        Result<string> first = await _cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("value");
        });
        Result<string> second = await _cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("other");
        });

        first.Value.Should().Be("value");
        second.Value.Should().Be("other");
        calls.Should().Be(2);
    }

    [Fact(DisplayName = "GetOrCreateAsync should pass cancellation token to factory")]
    public async Task GetOrCreateAsync_ShouldPassCancellationTokenToFactory()
    {
        CancellationTokenSource source = new();

        Result<string> result = await _cache.GetOrCreateAsync("key", token =>
        {
            token.CanBeCanceled.Should().BeTrue();
            token.Should().Be(source.Token);
            return ValueTask.FromResult("value");
        }, cancellationToken: source.Token);

        result.Value.Should().Be("value");
    }

    [Fact(DisplayName = "GetOrCreateAsync with blank key should return KeyRequired")]
    public async Task GetOrCreateAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        Result<string> result = await _cache.GetOrCreateAsync(
            "  ",
            _ => ValueTask.FromResult("value"));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "GetOrCreateAsync with null key should return KeyRequired")]
    public async Task GetOrCreateAsync_WithNullKey_ShouldReturnKeyRequired()
    {
        Result<string> result = await _cache.GetOrCreateAsync(
            null!,
            _ => ValueTask.FromResult("value"));

        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "GetOrCreateAsync with null factory should return FactoryRequired")]
    public async Task GetOrCreateAsync_WithNullFactory_ShouldReturnFactoryRequired()
    {
        Result<string> result = await _cache.GetOrCreateAsync<string>("key", null!);

        result.Errors[0].Code.Should().Be("Caching.Factory.Required");
    }

    [Fact(DisplayName = "SetAsync should succeed as a no-op")]
    public async Task SetAsync_ShouldSucceed()
    {
        Result result = await _cache.SetAsync("key", "value");

        result.IsSuccess.Should().BeTrue();
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

    [Fact(DisplayName = "RemoveAsync should succeed")]
    public async Task RemoveAsync_ShouldSucceed()
    {
        Result result = await _cache.RemoveAsync("key");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "RemoveAsync with blank key should return KeyRequired")]
    public async Task RemoveAsync_WithBlankKey_ShouldReturnKeyRequired()
    {
        Result result = await _cache.RemoveAsync("  ");

        result.Errors[0].Code.Should().Be("Caching.Key.Required");
    }

    [Fact(DisplayName = "RemoveByTagAsync with a single tag should succeed")]
    public async Task RemoveByTagAsync_WithSingleTag_ShouldSucceed()
    {
        Result result = await _cache.RemoveByTagAsync("tag");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "RemoveByTagAsync with a blank tag should return TagRequired")]
    public async Task RemoveByTagAsync_WithBlankTag_ShouldReturnTagRequired()
    {
        Result result = await _cache.RemoveByTagAsync("  ");

        result.Errors[0].Code.Should().Be("Caching.Tag.Required");
    }

    [Fact(DisplayName = "RemoveByTagAsync with a collection should succeed")]
    public async Task RemoveByTagAsync_WithCollection_ShouldSucceed()
    {
        Result result = await _cache.RemoveByTagAsync(["tag1", "tag2"]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "RemoveByTagAsync with an empty collection should succeed")]
    public async Task RemoveByTagAsync_WithEmptyCollection_ShouldSucceed()
    {
        Result result = await _cache.RemoveByTagAsync(Array.Empty<string>());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOrCreateAsync should respect custom options")]
    public async Task GetOrCreateAsync_ShouldRespectCustomOptions()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10),
            DisableLocalCache = true
        };

        Result<string> result = await _cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            options);

        result.Value.Should().Be("value");
    }

    [Fact(DisplayName = "SetAsync should respect custom options and tags")]
    public async Task SetAsync_ShouldRespectCustomOptionsAndTags()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10)
        };

        Result result = await _cache.SetAsync("key", "value", options, ["tag1", "tag2"]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "GetOrCreateAsync should reject invalid entry options")]
    public async Task GetOrCreateAsync_WithInvalidOptions_ShouldReturnEntryFailure()
    {
        CachingEntryOption options = new() { Expiration = TimeSpan.Zero };

        Result<string> result = await _cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            options);

        result.Errors[0].Code.Should().Be("Caching.Entry.ExpirationInvalid");
    }
}
