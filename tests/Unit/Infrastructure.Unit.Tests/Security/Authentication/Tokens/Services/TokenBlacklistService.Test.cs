using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Blacklist;
using Application.Common.Security.Authentication.Tokens.Options;

using Infrastructure.Security.Authentication.Tokens.Services;

namespace Infrastructure.Unit.Tests.Security.Authentication.Tokens.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class TokenBlacklistServiceTest
{
    private static readonly DateTimeOffset Now = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Error StoreError = Error.Unexpected("Test.StoreFailure", "Store failed.");

    private static TokenBlacklistService CreateService(
        FakeTokenBlacklistStore store,
        bool enabled = true)
    {
        return new TokenBlacklistService(
            store,
            new TokenBlacklistOption { Enabled = enabled },
            new FixedTimeProvider(Now));
    }

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "AddAsync should store the entry for the remaining token lifetime")]
    public async Task AddAsync_ShouldStoreRemainingLifetime()
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store);

        Result result = await service.AddAsync("jti-1", Now.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        store.Entries.Should().ContainKey("auth:blacklist:jti-1");
        store.Entries["auth:blacklist:jti-1"].Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact(DisplayName = "AddAsync should skip storage when the blacklist is disabled")]
    public async Task AddAsync_ShouldSkipWhenDisabled()
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store, enabled: false);

        Result result = await service.AddAsync("jti-1", Now.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        store.Entries.Should().BeEmpty();
    }

    [Fact(DisplayName = "AddAsync should skip an already expired access token")]
    public async Task AddAsync_ShouldSkipExpiredToken()
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store);

        Result result = await service.AddAsync("jti-1", Now.AddMinutes(-1));

        result.IsSuccess.Should().BeTrue();
        store.Entries.Should().BeEmpty();
    }

    [Theory(DisplayName = "AddAsync should reject a missing token identifier")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_ShouldRejectMissingJti(string? jti)
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store);

        Result result = await service.AddAsync(jti!, Now.AddMinutes(5));

        ShouldFailWith(result, "Token.Jti.Required");
    }

    [Fact(DisplayName = "AddAsync should reject a missing expiry")]
    public async Task AddAsync_ShouldRejectMissingExpiry()
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store);

        Result result = await service.AddAsync("jti-1", default);

        ShouldFailWith(result, "Token.Blacklist.ExpiryRequired");
    }

    [Fact(DisplayName = "AddAsync should reject an overlong token identifier")]
    public async Task AddAsync_ShouldRejectOverlongJti()
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store);
        string jti = new('a', TokenConstant.Limits.MaxJtiLength + 1);

        Result result = await service.AddAsync(jti, Now.AddMinutes(5));

        ShouldFailWith(result, "Token.Jti.TooLong");
    }

    [Fact(DisplayName = "AddAsync should propagate a store failure")]
    public async Task AddAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeTokenBlacklistStore { FailSet = true };
        TokenBlacklistService service = CreateService(store);

        Result result = await service.AddAsync("jti-1", Now.AddMinutes(5));

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "ContainsAsync should return the stored state")]
    public async Task ContainsAsync_ShouldReturnStoredState()
    {
        var store = new FakeTokenBlacklistStore();
        store.Entries["auth:blacklist:jti-1"] = TimeSpan.FromMinutes(5);
        TokenBlacklistService service = CreateService(store);

        Result<bool> contains = await service.ContainsAsync("jti-1");
        Result<bool> missing = await service.ContainsAsync("jti-2");

        contains.IsSuccess.Should().BeTrue();
        contains.Value.Should().BeTrue();
        missing.Value.Should().BeFalse();
    }

    [Fact(DisplayName = "ContainsAsync should return false when the blacklist is disabled")]
    public async Task ContainsAsync_ShouldReturnFalseWhenDisabled()
    {
        var store = new FakeTokenBlacklistStore();
        store.Entries["auth:blacklist:jti-1"] = TimeSpan.FromMinutes(5);
        TokenBlacklistService service = CreateService(store, enabled: false);

        Result<bool> result = await service.ContainsAsync("jti-1");

        result.Value.Should().BeFalse();
    }

    [Theory(DisplayName = "ContainsAsync should reject a missing token identifier")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ContainsAsync_ShouldRejectMissingJti(string? jti)
    {
        var store = new FakeTokenBlacklistStore();
        TokenBlacklistService service = CreateService(store);

        Result<bool> result = await service.ContainsAsync(jti!);

        ShouldFailWith(result, "Token.Jti.Required");
    }

    [Fact(DisplayName = "ContainsAsync should propagate a store failure")]
    public async Task ContainsAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeTokenBlacklistStore { FailExists = true };
        TokenBlacklistService service = CreateService(store);

        Result<bool> result = await service.ContainsAsync("jti-1");

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "The constructor should validate its dependencies and settings")]
    public void Constructor_ShouldValidateDependenciesAndSettings()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TokenBlacklistService(null!, new TokenBlacklistOption(), new FixedTimeProvider(Now)));
        Assert.Throws<ArgumentNullException>(
            () => new TokenBlacklistService(new FakeTokenBlacklistStore(), null!, new FixedTimeProvider(Now)));
        Assert.Throws<ArgumentNullException>(
            () => new TokenBlacklistService(new FakeTokenBlacklistStore(), new TokenBlacklistOption(), null!));
        Assert.Throws<InvalidOperationException>(
            () => new TokenBlacklistService(
                new FakeTokenBlacklistStore(),
                new TokenBlacklistOption { KeyPrefix = " " },
                new FixedTimeProvider(Now)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeTokenBlacklistStore : ITokenBlacklistStore
    {
        public Dictionary<string, TimeSpan> Entries { get; } = [];

        public bool FailSet { get; init; }

        public bool FailExists { get; init; }

        public Task<Result> SetAsync(
            string key,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            if (FailSet)
                return Task.FromResult(Result.Fail(StoreError));

            Entries[key] = lifetime;
            return Task.FromResult(Result.Ok());
        }

        public Task<Result<bool>> ExistsAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            return FailExists
                ? Task.FromResult(Result<bool>.Fail(StoreError))
                : Task.FromResult(Result<bool>.Ok(Entries.ContainsKey(key)));
        }
    }
}
