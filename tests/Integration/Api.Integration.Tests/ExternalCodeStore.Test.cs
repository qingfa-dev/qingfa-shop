using System.Security.Cryptography;
using System.Text;

using Application.Common.Security.Authentication.Identity;

using Core.Common.Monad.Results;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Time.Testing;

using Web.Api.Endpoints.Identity;

namespace Api.Integration.Tests;

/// <summary>Locks the one-time-code storage contract used by the external sign-in routes.</summary>
[Trait("Category", "Unit")]
public sealed class ExternalCodeStoreTest
{
    [Fact(DisplayName = "Stored codes should be 32-byte base64url values")]
    public async Task StoreAsync_ShouldReturnBase64UrlCode()
    {
        (ExternalCodeStore store, _, _) = CreateStore();

        Result<string> stored = await store.StoreAsync(
            CreateResponse(),
            TestContext.Current.CancellationToken);

        stored.IsSuccess.Should().BeTrue();
        stored.Value.Should().HaveLength(43);
        stored.Value.Should().NotContain("=");
        stored.Value.Should().NotContain("+");
        stored.Value.Should().NotContain("/");
    }

    [Fact(DisplayName = "A code should be exchangeable exactly once")]
    public async Task ConsumeAsync_ShouldReturnStoredResponseOnce()
    {
        (ExternalCodeStore store, _, _) = CreateStore();
        AuthTokenResponse response = CreateResponse();

        Result<string> stored = await store.StoreAsync(
            response,
            TestContext.Current.CancellationToken);

        Result<AuthTokenResponse> first = await store.ConsumeAsync(
            stored.Value,
            TestContext.Current.CancellationToken);

        first.IsSuccess.Should().BeTrue();
        first.Value.AccessToken.Should().Be(response.AccessToken);
        first.Value.RefreshToken.Should().Be(response.RefreshToken);
        first.Value.User.Should().Be(response.User);

        Result<AuthTokenResponse> replay = await store.ConsumeAsync(
            stored.Value,
            TestContext.Current.CancellationToken);

        replay.IsFailure.Should().BeTrue();
        replay.StatusCode.Should().Be(401);
        replay.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.CodeInvalid");
    }

    [Fact(DisplayName = "An unknown code should produce a 401 failure")]
    public async Task ConsumeAsync_WithUnknownCode_ShouldReturn401()
    {
        (ExternalCodeStore store, _, _) = CreateStore();

        Result<AuthTokenResponse> result = await store.ConsumeAsync(
            "not-a-real-code",
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.CodeInvalid");
    }

    [Fact(DisplayName = "A code should expire after the configured lifetime")]
    public async Task ConsumeAsync_AfterLifetime_ShouldReturn401()
    {
        (ExternalCodeStore store, _, FakeTimeProvider clock) = CreateStore();

        Result<string> stored = await store.StoreAsync(
            CreateResponse(),
            TestContext.Current.CancellationToken);

        clock.Advance(TimeSpan.FromSeconds(61));

        Result<AuthTokenResponse> result = await store.ConsumeAsync(
            stored.Value,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
    }

    [Fact(DisplayName = "The cache key should be the code hash, never the code itself")]
    public async Task StoreAsync_ShouldHashTheCodeInTheCacheKey()
    {
        (ExternalCodeStore store, FakeDistributedCache cache, _) = CreateStore();

        Result<string> stored = await store.StoreAsync(
            CreateResponse(),
            TestContext.Current.CancellationToken);

        cache.Keys.Should().ContainSingle();
        cache.Keys.Single().Should().StartWith("auth:external:");
        cache.Keys.Single().Should().NotContain(stored.Value);
        cache.Keys.Single().Should().HaveLength("auth:external:".Length + 64);
    }

    [Fact(DisplayName = "A malformed cached payload should produce an unexpected failure")]
    public async Task ConsumeAsync_WithMalformedPayload_ShouldReturnUnexpected()
    {
        (ExternalCodeStore store, FakeDistributedCache cache, _) = CreateStore();
        const string code = "malformed-code";

        cache.Set(
            KeyFor(code),
            Encoding.UTF8.GetBytes("{not-json"),
            new DistributedCacheEntryOptions());

        Result<AuthTokenResponse> result = await store.ConsumeAsync(
            code,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.CodeInvalid");
    }

    [Fact(DisplayName = "A null cached payload should produce an unexpected failure")]
    public async Task ConsumeAsync_WithNullPayload_ShouldReturnUnexpected()
    {
        (ExternalCodeStore store, FakeDistributedCache cache, _) = CreateStore();
        const string code = "null-code";

        cache.Set(
            KeyFor(code),
            "null"u8.ToArray(),
            new DistributedCacheEntryOptions());

        Result<AuthTokenResponse> result = await store.ConsumeAsync(
            code,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
    }

    [Fact(DisplayName = "A cache write failure should produce an unexpected failure")]
    public async Task StoreAsync_WhenCacheFails_ShouldReturnUnexpected()
    {
        var store = new ExternalCodeStore(
            new ThrowingDistributedCache(),
            new ExternalAuthOption());

        Result<string> result = await store.StoreAsync(
            CreateResponse(),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.CodeStoreFailed");
    }

    [Theory(DisplayName = "Blank codes should be rejected before touching the cache")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConsumeAsync_WithBlankCode_ShouldThrow(string code)
    {
        (ExternalCodeStore store, _, _) = CreateStore();

        Func<Task> act = () => store.ConsumeAsync(
            code,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static string KeyFor(string code) =>
        "auth:external:" + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static (ExternalCodeStore Store, FakeDistributedCache Cache, FakeTimeProvider Clock)
        CreateStore()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var cache = new FakeDistributedCache(clock);
        var store = new ExternalCodeStore(
            cache,
            new ExternalAuthOption { CodeLifetimeSeconds = 60 });

        return (store, cache, clock);
    }

    private static AuthTokenResponse CreateResponse() =>
        new(
            "access-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            "refresh-token",
            new IdentityUserModel(
                Guid.NewGuid(),
                "external-user",
                "external@example.com",
                "External",
                null,
                true,
                false));

    private sealed class FakeDistributedCache(FakeTimeProvider clock) : IDistributedCache
    {
        private readonly Dictionary<string, Entry> _entries = [];

        public IReadOnlyCollection<string> Keys => _entries.Keys;

        public byte[]? Get(string key) => TryGet(key)?.Value;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            DateTimeOffset? expiresAt = options.AbsoluteExpirationRelativeToNow is { } relative
                ? clock.GetUtcNow() + relative
                : options.AbsoluteExpiration;

            _entries[key] = new Entry([.. value], expiresAt);
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Remove(string key) => _entries.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        private Entry? TryGet(string key)
        {
            if (!_entries.TryGetValue(key, out Entry? entry))
                return null;

            if (entry.ExpiresAt is { } expiresAt && expiresAt <= clock.GetUtcNow())
            {
                _entries.Remove(key);
                return null;
            }

            return entry;
        }

        private sealed record Entry(byte[] Value, DateTimeOffset? ExpiresAt);
    }

    private sealed class ThrowingDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new InvalidOperationException("Cache is unavailable.");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            throw new InvalidOperationException("Cache is unavailable.");

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            throw new InvalidOperationException("Cache is unavailable.");

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) =>
            throw new InvalidOperationException("Cache is unavailable.");

        public void Refresh(string key) =>
            throw new InvalidOperationException("Cache is unavailable.");

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            throw new InvalidOperationException("Cache is unavailable.");

        public void Remove(string key) =>
            throw new InvalidOperationException("Cache is unavailable.");

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            throw new InvalidOperationException("Cache is unavailable.");
    }
}
