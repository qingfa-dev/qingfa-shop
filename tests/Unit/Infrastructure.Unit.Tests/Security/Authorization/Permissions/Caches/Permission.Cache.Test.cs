using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.Common.Caching;
using Application.Common.Security.Authorization.Options;
using Application.Common.Security.Authorization.Permissions.Caches;

using Infrastructure.Security.Authorization.Permissions.Caches;

namespace Infrastructure.Unit.Tests.Security.Authorization.Permissions.Caches;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionCache")]
public sealed class PermissionCacheTests
{
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IOptions<AuthzSetting>> _authzOptionsMock = new();
    private readonly AuthzSetting _authzSetting;
    private readonly PermissionCache _sut;

    public PermissionCacheTests()
    {
        _authzSetting = new AuthzSetting
        {
            PermissionCache = new PermissionCacheOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpiration = TimeSpan.FromMinutes(30),
            }
        };

        _authzOptionsMock.Setup(options => options.Value).Returns(_authzSetting);

        _sut = new PermissionCache(
            _cacheServiceMock.Object,
            _authzOptionsMock.Object,
            Mock.Of<ILogger<PermissionCache>>());
    }

    [Fact(DisplayName = "PermissionCacheEntry: Hit should expose a case-insensitive permission set")]
    public void PermissionCacheEntry_Hit_ShouldExposePermissions()
    {
        HashSet<string> permissions = ["perm1"];

        PermissionCacheEntry entry = PermissionCacheEntry.Hit(permissions);

        entry.IsHit.Should().BeTrue();
        entry.Permissions.Should().BeEquivalentTo(permissions);
        entry.Permissions!.Contains("PERM1").Should().BeTrue();
        PermissionCacheEntry.Miss.IsHit.Should().BeFalse();
        PermissionCacheEntry.Miss.Permissions.Should().BeNull();
    }

    [Fact(DisplayName = "PermissionCacheEntry: Hit should reject a null permission set")]
    public void PermissionCacheEntry_Hit_WithNull_ShouldThrow()
    {
        Action action = () => PermissionCacheEntry.Hit(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "PermissionCache: GetAsync should return a hit entry on cache hit")]
    public async Task GetAsync_ShouldReturnCachedPermissions_WhenCacheHit()
    {
        Guid userId = Guid.NewGuid();
        HashSet<string> expected = ["perm1", "perm2"];
        string expectedKey = $"perm:user:{userId}";

        _cacheServiceMock
            .Setup(service => service.GetOrCreateAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, ValueTask<PermissionCacheEntry>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(expected)));

        Result<PermissionCacheEntry> result = await _sut.GetAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHit.Should().BeTrue();
        result.Value.Permissions.Should().BeEquivalentTo(expected);
    }

    [Fact(DisplayName = "PermissionCache: GetAsync should return a miss entry with a short-lived marker")]
    public async Task GetAsync_ShouldReturnMiss_WhenCacheMiss()
    {
        Guid userId = Guid.NewGuid();
        string expectedKey = $"perm:user:{userId}";
        CachingEntryOption? capturedOptions = null;

        _cacheServiceMock
            .Setup(service => service.GetOrCreateAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, ValueTask<PermissionCacheEntry>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<CancellationToken, ValueTask<PermissionCacheEntry>>,
                CachingEntryOption?, IEnumerable<string>?, CancellationToken>(
                (_, _, options, _, _) => capturedOptions = options)
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));

        Result<PermissionCacheEntry> result = await _sut.GetAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHit.Should().BeFalse();
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Expiration.Should().Be(TimeSpan.FromSeconds(
            PermissionCacheConstant.Defaults.MissExpirationSeconds));
        capturedOptions.LocalCacheExpiration.Should().Be(capturedOptions.Expiration);
    }

    [Fact(DisplayName = "PermissionCache: GetAsync should wrap a cache failure in a permission read failure")]
    public async Task GetAsync_WhenCacheFails_ShouldReturnUserReadFailed()
    {
        Guid userId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, ValueTask<PermissionCacheEntry>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Fail(
                CachingOutcome.Failure.ReadFailed("perm:user:1")));

        Result<PermissionCacheEntry> result = await _sut.GetAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionCache.User.ReadFailed");
        result.Errors[0].Message.Should().Contain("Caching.Read.ProviderUnavailable");
    }

    [Fact(DisplayName = "PermissionCache: GetAsync with an empty user id should fail without touching the cache")]
    public async Task GetAsync_WithEmptyUserId_ShouldReturnUserIdRequired()
    {
        Result<PermissionCacheEntry> result = await _sut.GetAsync(
            Guid.Empty,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionCache.UserId.Required");
        _cacheServiceMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "PermissionCache: SetUserAsync should store permissions with the configured TTL and tags")]
    public async Task SetUserAsync_ShouldStorePermissions_WithConfiguredTtlAndTags()
    {
        Guid userId = Guid.NewGuid();
        HashSet<string> permissions = ["perm1"];
        string expectedKey = $"perm:user:{userId}";

        _cacheServiceMock
            .Setup(service => service.SetAsync(
                expectedKey,
                It.IsAny<PermissionCacheEntry>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.SetUserAsync(
            userId,
            permissions,
            ct: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheServiceMock.Verify(service => service.SetAsync(
            expectedKey,
            It.Is<PermissionCacheEntry>(entry => entry.IsHit && entry.Permissions!.SetEquals(permissions)),
            It.Is<CachingEntryOption?>(options =>
                options != null &&
                options.Expiration == _authzSetting.PermissionCache.AbsoluteExpiration &&
                options.LocalCacheExpiration == _authzSetting.PermissionCache.SlidingExpiration),
            It.Is<IEnumerable<string>?>(tags =>
                tags != null &&
                tags.Contains($"perm:user:{userId}") &&
                tags.Contains("perm:global")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PermissionCache: SetUserAsync should include role tags when role IDs are provided")]
    public async Task SetUserAsync_ShouldIncludeRoleTags_WhenRoleIdsProvided()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId1 = Guid.NewGuid();
        Guid roleId2 = Guid.NewGuid();
        HashSet<string> permissions = ["perm1"];

        _cacheServiceMock
            .Setup(service => service.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PermissionCacheEntry>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.SetUserAsync(
            userId,
            permissions,
            [roleId1, roleId2],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheServiceMock.Verify(service => service.SetAsync(
            It.IsAny<string>(),
            It.Is<PermissionCacheEntry>(entry => entry.Permissions!.SetEquals(permissions)),
            It.IsAny<CachingEntryOption?>(),
            It.Is<IEnumerable<string>?>(tags =>
                tags != null &&
                tags.Contains($"perm:role:{roleId1}") &&
                tags.Contains($"perm:role:{roleId2}")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PermissionCache: SetUserAsync should wrap a cache failure in a permission write failure")]
    public async Task SetUserAsync_WhenCacheFails_ShouldReturnUserWriteFailed()
    {
        Guid userId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PermissionCacheEntry>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.WriteFailed("key")));

        Result result = await _sut.SetUserAsync(
            userId,
            ["perm1"],
            ct: TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionCache.User.WriteFailed");
    }

    [Fact(DisplayName = "PermissionCache: SetUserAsync should reject invalid identifiers and null sets")]
    public async Task SetUserAsync_ShouldValidateArguments()
    {
        Result emptyUser = await _sut.SetUserAsync(Guid.Empty, ["perm1"]);
        Result nullPermissions = await _sut.SetUserAsync(Guid.NewGuid(), null!);

        emptyUser.Errors[0].Code.Should().Be("PermissionCache.UserId.Required");
        nullPermissions.Errors[0].Code.Should().Be("PermissionCache.Permissions.Required");
    }

    [Fact(DisplayName = "PermissionCache: InvalidateUserAsync should remove the key and tag")]
    public async Task InvalidateUserAsync_ShouldRemoveKeyAndTag()
    {
        Guid userId = Guid.NewGuid();
        string expectedKey = $"perm:user:{userId}";
        string expectedTag = $"perm:user:{userId}";

        _cacheServiceMock
            .Setup(service => service.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheServiceMock
            .Setup(service => service.RemoveByTagAsync(expectedTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.InvalidateUserAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheServiceMock.Verify(
            service => service.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheServiceMock.Verify(
            service => service.RemoveByTagAsync(expectedTag, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionCache: InvalidateUserAsync should report a key-removal failure")]
    public async Task InvalidateUserAsync_WhenRemoveFails_ShouldReturnInvalidationFailed()
    {
        Guid userId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.RemoveFailed("key")));

        Result result = await _sut.InvalidateUserAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.User.InvalidationFailed");
        _cacheServiceMock.Verify(
            service => service.RemoveByTagAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionCache: InvalidateUserAsync should report a tag-eviction failure")]
    public async Task InvalidateUserAsync_WhenEvictFails_ShouldReturnInvalidationFailed()
    {
        Guid userId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheServiceMock
            .Setup(service => service.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.EvictFailed("tag")));

        Result result = await _sut.InvalidateUserAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.User.InvalidationFailed");
    }

    [Fact(DisplayName = "PermissionCache: InvalidateAllAsync should remove by global tag")]
    public async Task InvalidateAllAsync_ShouldRemoveByGlobalTag()
    {
        _cacheServiceMock
            .Setup(service => service.RemoveByTagAsync("perm:global", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.InvalidateAllAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheServiceMock.Verify(
            service => service.RemoveByTagAsync("perm:global", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionCache: InvalidateAllAsync should report an eviction failure")]
    public async Task InvalidateAllAsync_WhenEvictFails_ShouldReturnAllInvalidationFailed()
    {
        _cacheServiceMock
            .Setup(service => service.RemoveByTagAsync("perm:global", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.EvictFailed(tag: null)));

        Result result = await _sut.InvalidateAllAsync(TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.All.InvalidationFailed");
    }

    [Fact(DisplayName = "PermissionCache: GetRoleAsync should return a hit entry on cache hit")]
    public async Task GetRoleAsync_ShouldReturnCachedPermissions_WhenCacheHit()
    {
        Guid roleId = Guid.NewGuid();
        HashSet<string> expected = ["role.perm1"];
        string expectedKey = $"perm:role:{roleId}";

        _cacheServiceMock
            .Setup(service => service.GetOrCreateAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, ValueTask<PermissionCacheEntry>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(expected)));

        Result<PermissionCacheEntry> result = await _sut.GetRoleAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHit.Should().BeTrue();
        result.Value.Permissions.Should().BeEquivalentTo(expected);
    }

    [Fact(DisplayName = "PermissionCache: GetRoleAsync should return a miss entry on cache miss")]
    public async Task GetRoleAsync_ShouldReturnMiss_WhenCacheMiss()
    {
        Guid roleId = Guid.NewGuid();
        string expectedKey = $"perm:role:{roleId}";

        _cacheServiceMock
            .Setup(service => service.GetOrCreateAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, ValueTask<PermissionCacheEntry>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));

        Result<PermissionCacheEntry> result = await _sut.GetRoleAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHit.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionCache: GetRoleAsync should wrap a cache failure in a role read failure")]
    public async Task GetRoleAsync_WhenCacheFails_ShouldReturnRoleReadFailed()
    {
        Guid roleId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, ValueTask<PermissionCacheEntry>>>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Fail(
                CachingOutcome.Failure.ReadFailed("perm:role:1")));

        Result<PermissionCacheEntry> result = await _sut.GetRoleAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.Role.ReadFailed");
    }

    [Fact(DisplayName = "PermissionCache: GetRoleAsync with an empty role id should fail without touching the cache")]
    public async Task GetRoleAsync_WithEmptyRoleId_ShouldReturnRoleIdRequired()
    {
        Result<PermissionCacheEntry> result = await _sut.GetRoleAsync(
            Guid.Empty,
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.RoleId.Required");
        _cacheServiceMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "PermissionCache: SetRoleAsync should store permissions with the configured TTL and tags")]
    public async Task SetRoleAsync_ShouldStorePermissions_WithConfiguredTtlAndTags()
    {
        Guid roleId = Guid.NewGuid();
        HashSet<string> permissions = ["role.perm1"];
        string expectedKey = $"perm:role:{roleId}";

        _cacheServiceMock
            .Setup(service => service.SetAsync(
                expectedKey,
                It.IsAny<PermissionCacheEntry>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.SetRoleAsync(
            roleId,
            permissions,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheServiceMock.Verify(service => service.SetAsync(
            expectedKey,
            It.Is<PermissionCacheEntry>(entry => entry.IsHit && entry.Permissions!.SetEquals(permissions)),
            It.Is<CachingEntryOption?>(options =>
                options != null &&
                options.Expiration == _authzSetting.PermissionCache.AbsoluteExpiration &&
                options.LocalCacheExpiration == _authzSetting.PermissionCache.SlidingExpiration),
            It.Is<IEnumerable<string>?>(tags =>
                tags != null &&
                tags.Contains($"perm:role:{roleId}") &&
                tags.Contains("perm:global")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "PermissionCache: SetRoleAsync should wrap a cache failure in a role write failure")]
    public async Task SetRoleAsync_WhenCacheFails_ShouldReturnRoleWriteFailed()
    {
        Guid roleId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PermissionCacheEntry>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.WriteFailed("key")));

        Result result = await _sut.SetRoleAsync(
            roleId,
            ["role.perm1"],
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.Role.WriteFailed");
    }

    [Fact(DisplayName = "PermissionCache: SetRoleAsync should validate arguments")]
    public async Task SetRoleAsync_ShouldValidateArguments()
    {
        Result emptyRole = await _sut.SetRoleAsync(Guid.Empty, ["role.perm1"]);
        Result nullPermissions = await _sut.SetRoleAsync(Guid.NewGuid(), null!);

        emptyRole.Errors[0].Code.Should().Be("PermissionCache.RoleId.Required");
        nullPermissions.Errors[0].Code.Should().Be("PermissionCache.Permissions.Required");
    }

    [Fact(DisplayName = "PermissionCache: InvalidateRoleAsync should remove the role key and tag")]
    public async Task InvalidateRoleAsync_ShouldRemoveRoleKeyAndTag()
    {
        Guid roleId = Guid.NewGuid();
        string expectedKey = $"perm:role:{roleId}";
        string expectedTag = $"perm:role:{roleId}";

        _cacheServiceMock
            .Setup(service => service.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheServiceMock
            .Setup(service => service.RemoveByTagAsync(expectedTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.InvalidateRoleAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheServiceMock.Verify(
            service => service.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheServiceMock.Verify(
            service => service.RemoveByTagAsync(expectedTag, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionCache: InvalidateRoleAsync should report a removal failure")]
    public async Task InvalidateRoleAsync_WhenRemoveFails_ShouldReturnInvalidationFailed()
    {
        Guid roleId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.RemoveFailed("key")));

        Result result = await _sut.InvalidateRoleAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.Role.InvalidationFailed");
        _cacheServiceMock.Verify(
            service => service.RemoveByTagAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionCache: InvalidateRoleAsync should report a tag-eviction failure")]
    public async Task InvalidateRoleAsync_WhenEvictFails_ShouldReturnInvalidationFailed()
    {
        Guid roleId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(service => service.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheServiceMock
            .Setup(service => service.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(CachingOutcome.Failure.EvictFailed("tag")));

        Result result = await _sut.InvalidateRoleAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.Errors[0].Code.Should().Be("PermissionCache.Role.InvalidationFailed");
    }

    [Fact(DisplayName = "PermissionCache: SetUserAsync should use the TTL from the current options snapshot")]
    public async Task SetUserAsync_ShouldUseConfiguredTtl()
    {
        Guid userId = Guid.NewGuid();
        HashSet<string> permissions = ["perm1"];

        _authzOptionsMock.Setup(options => options.Value).Returns(new AuthzSetting
        {
            PermissionCache = new PermissionCacheOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10),
                AbsoluteExpiration = TimeSpan.FromHours(1),
            }
        });

        _cacheServiceMock
            .Setup(service => service.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PermissionCacheEntry>(),
                It.IsAny<CachingEntryOption?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        await _sut.SetUserAsync(userId, permissions, ct: TestContext.Current.CancellationToken);

        _cacheServiceMock.Verify(service => service.SetAsync(
            It.IsAny<string>(),
            It.Is<PermissionCacheEntry>(entry => entry.Permissions!.SetEquals(permissions)),
            It.Is<CachingEntryOption?>(options =>
                options != null &&
                options.Expiration == TimeSpan.FromHours(1) &&
                options.LocalCacheExpiration == TimeSpan.FromMinutes(10)),
            It.IsAny<IEnumerable<string>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
