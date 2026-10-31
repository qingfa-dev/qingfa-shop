using Microsoft.Extensions.Logging;

using Application.BuildingBlocks.Security.Authorization.Permissions.Caches;
using Application.BuildingBlocks.Security.Authorization.Permissions.Store;

using Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Permissions.Services;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authorization.Permissions.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionService")]
public sealed class PermissionServiceTests
{
    private readonly Mock<IPermissionCache> _cacheMock = new();
    private readonly Mock<IPermissionStore> _storeMock = new();
    private readonly Mock<ILogger<PermissionService>> _loggerMock = new();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _sut = new PermissionService(_cacheMock.Object, _storeMock.Object, _loggerMock.Object);

        _cacheMock
            .Setup(cache => cache.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));
        _cacheMock
            .Setup(cache => cache.GetRoleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));
        _cacheMock
            .Setup(cache => cache.SetRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<HashSet<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.SetUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<HashSet<string>>(),
                It.IsAny<IEnumerable<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should return the cached set on a cache hit")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldReturnCached_WhenCacheHit()
    {
        Guid userId = Guid.NewGuid();
        HashSet<string> cachedPermissions = ["perm1", "perm2"];

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(cachedPermissions)));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedPermissions);
        _storeMock.Verify(
            store => store.GetUserRoleIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should resolve from the store on a cache miss")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldResolveFromStore_WhenCacheMiss()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        HashSet<string> rolePermissions = ["role.perm1"];

        SetupMiss(userId);
        SetupRoleMiss(roleId);
        _storeMock
            .Setup(store => store.GetUserRoleIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<Guid>>.Ok([roleId]));
        _storeMock
            .Setup(store => store.GetUserDirectPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok([]));
        _storeMock
            .Setup(store => store.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(rolePermissions));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("role.perm1");
        _cacheMock.Verify(
            cache => cache.SetRoleAsync(roleId, rolePermissions, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheMock.Verify(
            cache => cache.SetUserAsync(
                userId,
                It.Is<HashSet<string>>(permissions => permissions.Contains("role.perm1")),
                It.Is<IEnumerable<Guid>?>(ids => ids != null && ids.Contains(roleId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should merge role and direct permissions")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldMergeRoleAndDirectPermissions()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        HashSet<string> rolePermissions = ["role.perm1", "shared.perm"];
        HashSet<string> directPermissions = ["direct.perm1", "SHARED.PERM"];

        SetupMiss(userId);
        SetupRoleMiss(roleId);
        _storeMock
            .Setup(store => store.GetUserRoleIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<Guid>>.Ok([roleId]));
        _storeMock
            .Setup(store => store.GetUserDirectPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(directPermissions));
        _storeMock
            .Setup(store => store.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(rolePermissions));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("role.perm1");
        result.Value.Should().Contain("direct.perm1");
        result.Value.Should().Contain("SHARED.PERM");
        result.Value.Should().HaveCount(3);
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should use cached role permissions")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldUseCachedRolePermissions()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        HashSet<string> rolePermissions = ["role.perm1"];

        SetupMiss(userId);
        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(rolePermissions)));
        _storeMock
            .Setup(store => store.GetUserRoleIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<Guid>>.Ok([roleId]));
        _storeMock
            .Setup(store => store.GetUserDirectPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok([]));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().Contain("role.perm1");
        _storeMock.Verify(
            store => store.GetRolePermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should deny with an empty set when the store fails")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldReturnEmpty_WhenStoreFails()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        Error failure = Error.Unexpected("Store.Fail", "Store unavailable.");

        SetupMiss(userId);
        SetupRoleMiss(roleId);
        _storeMock
            .Setup(store => store.GetUserRoleIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<Guid>>.Ok([roleId]));
        _storeMock
            .Setup(store => store.GetUserDirectPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Fail(failure));
        _storeMock
            .Setup(store => store.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Fail(failure));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should never throw when the cache fails")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldReturnEmpty_WhenCacheThrows()
    {
        Guid userId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cache down"));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should fall back to the store when the cache returns a failure")]
    public async Task GetEffectiveUserPermissionsAsync_ShouldFallBack_WhenCacheReturnsFailure()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Fail(
                PermissionCacheOutcome.Failure.UserReadFailed(userId, [])));
        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Fail(
                PermissionCacheOutcome.Failure.RoleReadFailed(roleId, [])));
        _cacheMock
            .Setup(cache => cache.SetRoleAsync(roleId, It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(
                PermissionCacheOutcome.Failure.RoleWriteFailed(roleId, [])));
        _cacheMock
            .Setup(cache => cache.SetUserAsync(
                userId,
                It.IsAny<HashSet<string>>(),
                It.IsAny<IEnumerable<Guid>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(
                PermissionCacheOutcome.Failure.UserWriteFailed(userId, [])));

        _storeMock
            .Setup(store => store.GetUserRoleIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<Guid>>.Ok([roleId]));
        _storeMock
            .Setup(store => store.GetUserDirectPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok([]));
        _storeMock
            .Setup(store => store.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(["role.perm1"]));

        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("role.perm1");
    }

    [Fact(DisplayName = "PermissionService: GetEffectiveUserPermissions should reject an empty user id")]
    public async Task GetEffectiveUserPermissionsAsync_WithEmptyUserId_ShouldReturnUserIdRequired()
    {
        Result<HashSet<string>> result = await _sut.GetEffectiveUserPermissionsAsync(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionService.UserId.Required");
        _cacheMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "PermissionService: GetRolePermissions should return cached permissions on a cache hit")]
    public async Task GetRolePermissionsAsync_ShouldReturnCached_WhenCacheHit()
    {
        Guid roleId = Guid.NewGuid();
        HashSet<string> rolePermissions = ["role.perm1"];

        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(rolePermissions)));

        Result<HashSet<string>> result = await _sut.GetRolePermissionsAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.Value.Should().BeEquivalentTo(rolePermissions);
        _storeMock.Verify(
            store => store.GetRolePermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: GetRolePermissions should load and cache on a cache miss")]
    public async Task GetRolePermissionsAsync_ShouldLoadAndCache_WhenCacheMiss()
    {
        Guid roleId = Guid.NewGuid();
        HashSet<string> rolePermissions = ["role.perm1"];

        SetupRoleMiss(roleId);
        _storeMock
            .Setup(store => store.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Ok(rolePermissions));

        Result<HashSet<string>> result = await _sut.GetRolePermissionsAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.Value.Should().BeEquivalentTo(rolePermissions);
        _cacheMock.Verify(
            cache => cache.SetRoleAsync(roleId, rolePermissions, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: GetRolePermissions should not cache a failed store read")]
    public async Task GetRolePermissionsAsync_ShouldNotCache_WhenStoreFails()
    {
        Guid roleId = Guid.NewGuid();

        SetupRoleMiss(roleId);
        _storeMock
            .Setup(store => store.GetRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HashSet<string>>.Fail(
                Error.Unexpected("Store.Fail", "Store unavailable.")));

        Result<HashSet<string>> result = await _sut.GetRolePermissionsAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _cacheMock.Verify(
            cache => cache.SetRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<HashSet<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: GetRolePermissions should never throw when the cache fails")]
    public async Task GetRolePermissionsAsync_ShouldReturnEmpty_WhenCacheThrows()
    {
        Guid roleId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cache down"));

        Result<HashSet<string>> result = await _sut.GetRolePermissionsAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionService: HasAnyPermissions should return true when at least one permission is held")]
    public async Task HasAnyPermissionsAsync_ShouldReturnTrue_WhenAnyHeld()
    {
        Guid userId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(["perm1"])));

        Result<bool> result = await _sut.HasAnyPermissionsAsync(
            userId,
            ["perm2", "PERM1"],
            TestContext.Current.CancellationToken);

        result.Value.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionService: HasAnyPermissions should return false when none are held")]
    public async Task HasAnyPermissionsAsync_ShouldReturnFalse_WhenNoneHeld()
    {
        Guid userId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(["perm1"])));

        Result<bool> result = await _sut.HasAnyPermissionsAsync(
            userId,
            ["perm2", "perm3"],
            TestContext.Current.CancellationToken);

        result.Value.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionService: HasAnyPermissions should reject a null collection")]
    public async Task HasAnyPermissionsAsync_WithNull_ShouldReturnPermissionsRequired()
    {
        Result<bool> result = await _sut.HasAnyPermissionsAsync(Guid.NewGuid(), null!);

        result.Errors[0].Code.Should().Be("PermissionService.Permissions.Required");
    }

    [Fact(DisplayName = "PermissionService: RoleHasAnyPermissions should evaluate role permissions")]
    public async Task RoleHasAnyPermissionsAsync_ShouldEvaluateRolePermissions()
    {
        Guid roleId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(["role.perm1"])));

        Result<bool> granted = await _sut.RoleHasAnyPermissionsAsync(
            roleId,
            ["role.perm2", "ROLE.PERM1"],
            TestContext.Current.CancellationToken);
        Result<bool> denied = await _sut.RoleHasAnyPermissionsAsync(
            roleId,
            ["role.perm2"],
            TestContext.Current.CancellationToken);

        granted.Value.Should().BeTrue();
        denied.Value.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionService: RoleHasAnyPermissions should reject a null collection")]
    public async Task RoleHasAnyPermissionsAsync_WithNull_ShouldReturnPermissionsRequired()
    {
        Result<bool> result = await _sut.RoleHasAnyPermissionsAsync(Guid.NewGuid(), null!);

        result.Errors[0].Code.Should().Be("PermissionService.Permissions.Required");
    }

    [Fact(DisplayName = "PermissionService: HasAllPermissions should return true when every permission is held")]
    public async Task HasAllPermissionsAsync_ShouldReturnTrue_WhenAllHeld()
    {
        Guid userId = Guid.NewGuid();
        HashSet<string> permissions = ["perm1", "perm2"];

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(permissions)));

        Result<bool> result = await _sut.HasAllPermissionsAsync(
            userId,
            ["PERM1", "perm2"],
            TestContext.Current.CancellationToken);

        result.Value.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionService: HasAllPermissions should return false when a permission is missing")]
    public async Task HasAllPermissionsAsync_ShouldReturnFalse_WhenPermissionMissing()
    {
        Guid userId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(["perm1"])));

        Result<bool> result = await _sut.HasAllPermissionsAsync(
            userId,
            ["perm1", "perm2"],
            TestContext.Current.CancellationToken);

        result.Value.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionService: HasAllPermissions should reject a null collection")]
    public async Task HasAllPermissionsAsync_WithNull_ShouldReturnPermissionsRequired()
    {
        Result<bool> result = await _sut.HasAllPermissionsAsync(Guid.NewGuid(), null!);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionService.Permissions.Required");
    }

    [Fact(DisplayName = "PermissionService: RoleHasAllPermissions should evaluate role permissions")]
    public async Task RoleHasAllPermissionsAsync_ShouldEvaluateRolePermissions()
    {
        Guid roleId = Guid.NewGuid();
        HashSet<string> permissions = ["role.perm1"];

        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Hit(permissions)));

        Result<bool> granted = await _sut.RoleHasAllPermissionsAsync(
            roleId,
            ["ROLE.PERM1"],
            TestContext.Current.CancellationToken);
        Result<bool> denied = await _sut.RoleHasAllPermissionsAsync(
            roleId,
            ["role.perm2"],
            TestContext.Current.CancellationToken);

        granted.Value.Should().BeTrue();
        denied.Value.Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionService: RoleHasAllPermissions should reject a null collection")]
    public async Task RoleHasAllPermissionsAsync_WithNull_ShouldReturnPermissionsRequired()
    {
        Result<bool> result = await _sut.RoleHasAllPermissionsAsync(Guid.NewGuid(), null!);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionService.Permissions.Required");
    }

    [Fact(DisplayName = "PermissionService: invalidation should delegate to the cache")]
    public async Task InvalidationMethods_ShouldDelegateToCache()
    {
        Guid roleId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        (await _sut.InvalidateRolePermissionsAsync(roleId, TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();
        (await _sut.InvalidateUserPermissionsAsync(userId, TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();

        _cacheMock.Verify(
            cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheMock.Verify(
            cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: invalidation should surface cache failures with service context")]
    public async Task InvalidationMethods_WhenCacheFails_ShouldReturnServiceFailure()
    {
        Guid roleId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _cacheMock
            .Setup(cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(
                PermissionCacheOutcome.Failure.RoleInvalidationFailed(roleId, [])));
        _cacheMock
            .Setup(cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(
                PermissionCacheOutcome.Failure.UserInvalidationFailed(userId, [])));

        Result roleResult = await _sut.InvalidateRolePermissionsAsync(
            roleId,
            TestContext.Current.CancellationToken);
        Result userResult = await _sut.InvalidateUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        roleResult.Errors[0].Code.Should().Be("PermissionService.Role.InvalidationFailed");
        userResult.Errors[0].Code.Should().Be("PermissionService.User.InvalidationFailed");
    }

    [Fact(DisplayName = "PermissionService: invalidations should reject empty identifiers")]
    public async Task InvalidationMethods_WithEmptyIdentifier_ShouldReturnIdentifierRequired()
    {
        (await _sut.InvalidateRolePermissionsAsync(Guid.Empty))
            .Errors[0].Code.Should().Be("PermissionService.RoleId.Required");
        (await _sut.InvalidateUserPermissionsAsync(Guid.Empty))
            .Errors[0].Code.Should().Be("PermissionService.UserId.Required");
    }

    [Fact(DisplayName = "PermissionService: AddRolePermissions should invalidate the role cache on success")]
    public async Task AddRolePermissionsAsync_ShouldInvalidateRoleCache_WhenStoreSucceeds()
    {
        Guid roleId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.AddRolePermissionsAsync(
                roleId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.AddRolePermissionsAsync(
            roleId,
            ["identity.users.all.read"],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: AddRolePermissions should return the store failure without invalidating")]
    public async Task AddRolePermissionsAsync_ShouldReturnFailure_WhenStoreFails()
    {
        Guid roleId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.AddRolePermissionsAsync(
                roleId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected("Store.Fail", "Store unavailable.")));

        Result result = await _sut.AddRolePermissionsAsync(
            roleId,
            ["identity.users.all.read"],
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateRoleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: RemoveRolePermissions should invalidate the role cache on success")]
    public async Task RemoveRolePermissionsAsync_ShouldInvalidateRoleCache_WhenStoreSucceeds()
    {
        Guid roleId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.RemoveRolePermissionsAsync(
                roleId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.RemoveRolePermissionsAsync(
            roleId,
            ["identity.users.all.read"],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateRoleAsync(roleId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: RemoveRolePermissions should return the store failure without invalidating")]
    public async Task RemoveRolePermissionsAsync_ShouldReturnFailure_WhenStoreFails()
    {
        Guid roleId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.RemoveRolePermissionsAsync(
                roleId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected("Store.Fail", "Store unavailable.")));

        Result result = await _sut.RemoveRolePermissionsAsync(
            roleId,
            ["identity.users.all.read"],
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateRoleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: AddUserDirectPermissions should invalidate the user cache on success")]
    public async Task AddUserDirectPermissionsAsync_ShouldInvalidateUserCache_WhenStoreSucceeds()
    {
        Guid userId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.AddUserDirectPermissionsAsync(
                userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.AddUserDirectPermissionsAsync(
            userId,
            ["todo.todos.item.read"],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: AddUserDirectPermissions should return the store failure without invalidating")]
    public async Task AddUserDirectPermissionsAsync_ShouldReturnFailure_WhenStoreFails()
    {
        Guid userId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.AddUserDirectPermissionsAsync(
                userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected("Store.Fail", "Store unavailable.")));

        Result result = await _sut.AddUserDirectPermissionsAsync(
            userId,
            ["todo.todos.item.read"],
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "PermissionService: RemoveUserDirectPermissions should invalidate the user cache on success")]
    public async Task RemoveUserDirectPermissionsAsync_ShouldInvalidateUserCache_WhenStoreSucceeds()
    {
        Guid userId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.RemoveUserDirectPermissionsAsync(
                userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _cacheMock
            .Setup(cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await _sut.RemoveUserDirectPermissionsAsync(
            userId,
            ["todo.todos.item.read"],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "PermissionService: RemoveUserDirectPermissions should return the store failure without invalidating")]
    public async Task RemoveUserDirectPermissionsAsync_ShouldReturnFailure_WhenStoreFails()
    {
        Guid userId = Guid.NewGuid();

        _storeMock
            .Setup(store => store.RemoveUserDirectPermissionsAsync(
                userId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected("Store.Fail", "Store unavailable.")));

        Result result = await _sut.RemoveUserDirectPermissionsAsync(
            userId,
            ["todo.todos.item.read"],
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _cacheMock.Verify(
            cache => cache.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupMiss(Guid userId)
    {
        _cacheMock
            .Setup(cache => cache.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));
    }

    private void SetupRoleMiss(Guid roleId)
    {
        _cacheMock
            .Setup(cache => cache.GetRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));
    }
}
