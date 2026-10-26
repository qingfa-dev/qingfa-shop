using Application.Common.Caching;

using Infrastructure.Caching.Services;

namespace Infrastructure.Unit.Tests.Caching;

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
        string first = await _cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("value");
        });
        string second = await _cache.GetOrCreateAsync("key", _ =>
        {
            calls++;
            return ValueTask.FromResult("other");
        });

        first.Should().Be("value");
        second.Should().Be("other");
        calls.Should().Be(2);
    }

    [Fact(DisplayName = "GetOrCreateAsync should pass cancellation token to factory")]
    public async Task GetOrCreateAsync_ShouldPassCancellationTokenToFactory()
    {
        CancellationTokenSource source = new();

        await _cache.GetOrCreateAsync("key", token =>
        {
            token.CanBeCanceled.Should().BeTrue();
            token.Should().Be(source.Token);
            return ValueTask.FromResult("value");
        }, cancellationToken: source.Token);
    }

    [Fact(DisplayName = "GetOrCreateAsync with blank key should throw")]
    public async Task GetOrCreateAsync_WithBlankKey_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.GetOrCreateAsync(
            "  ",
            _ => ValueTask.FromResult("value"));

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Key.Required*");
    }

    [Fact(DisplayName = "GetOrCreateAsync with null key should throw")]
    public async Task GetOrCreateAsync_WithNullKey_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.GetOrCreateAsync(
            null!,
            _ => ValueTask.FromResult("value"));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact(DisplayName = "GetOrCreateAsync with null factory should throw")]
    public async Task GetOrCreateAsync_WithNullFactory_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.GetOrCreateAsync<string>(
            "key",
            null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("*Caching.Factory.Required*");
    }

    [Fact(DisplayName = "SetAsync should not throw and should be a no-op")]
    public async Task SetAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.SetAsync("key", "value");

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "SetAsync with blank key should throw")]
    public async Task SetAsync_WithBlankKey_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.SetAsync("  ", "value");

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Key.Required*");
    }

    [Fact(DisplayName = "SetAsync with null value should throw")]
    public async Task SetAsync_WithNullValue_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.SetAsync<string>("key", null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("*Caching.Value.Required*");
    }

    [Fact(DisplayName = "RemoveAsync should not throw")]
    public async Task RemoveAsync_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.RemoveAsync("key");

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "RemoveAsync with blank key should throw")]
    public async Task RemoveAsync_WithBlankKey_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.RemoveAsync("  ");

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Key.Required*");
    }

    [Fact(DisplayName = "RemoveByTagAsync with single tag should not throw")]
    public async Task RemoveByTagAsync_WithSingleTag_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.RemoveByTagAsync("tag");

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "RemoveByTagAsync with blank single tag should throw")]
    public async Task RemoveByTagAsync_WithBlankTag_ShouldThrow()
    {
        Func<Task> act = async () => await _cache.RemoveByTagAsync("  ");

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Caching.Tag.Required*");
    }

    [Fact(DisplayName = "RemoveByTagAsync with collection should not throw")]
    public async Task RemoveByTagAsync_WithCollection_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.RemoveByTagAsync(new[] { "tag1", "tag2" });

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "RemoveByTagAsync with empty collection should not throw")]
    public async Task RemoveByTagAsync_WithEmptyCollection_ShouldNotThrow()
    {
        Func<Task> act = async () => await _cache.RemoveByTagAsync(Array.Empty<string>());

        await act.Should().NotThrowAsync();
    }


    [Fact(DisplayName = "GetOrCreateAsync should respect custom options")]
    public async Task GetOrCreateAsync_ShouldRespectCustomOptions()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10),
            DisableLocalCache = true
        };

        string value = await _cache.GetOrCreateAsync(
            "key",
            _ => ValueTask.FromResult("value"),
            options);

        value.Should().Be("value");
    }

    [Fact(DisplayName = "SetAsync should respect custom options and tags")]
    public async Task SetAsync_ShouldRespectCustomOptionsAndTags()
    {
        CachingEntryOption options = new()
        {
            Expiration = TimeSpan.FromMinutes(10)
        };

        await _cache.SetAsync("key", "value", options, new[] { "tag1", "tag2" });
    }
}
