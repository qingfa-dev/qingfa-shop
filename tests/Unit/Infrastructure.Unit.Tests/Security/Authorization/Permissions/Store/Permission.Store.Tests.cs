using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Application.Common.Security.Authorization.Domain;
using Application.Common.Security.Authorization.Permissions.Store;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Authorization.Permissions.Store;
using Infrastructure.Security.Identity.Domain.Roles.Claims;
using Infrastructure.Security.Identity.Domain.Users.Claims;
using Infrastructure.Security.Identity.Domain.Users.Roles;

namespace Infrastructure.Unit.Tests.Security.Authorization.Permissions.Store;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionStore")]
public sealed class PermissionStoreTests : IDisposable
{
    private const string ReadPermission = "store.catalog.product.read";
    private const string CreatePermission = "store.catalog.product.create";
    private const string DeletePermission = "store.catalog.product.delete";

    private readonly ApplicationDbContext _dbContext;
    private readonly PermissionStoreService _sut;

    public PermissionStoreTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _sut = new PermissionStoreService(_dbContext, Mock.Of<ILogger<PermissionStoreService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "PermissionStore: should return an empty set when the user has no permissions")]
    public async Task GetUserPermissionsAsync_ShouldReturnEmpty_WhenNoPermissions()
    {
        Result<HashSet<string>> result = await _sut.GetUserPermissionsAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionStore: should read permissions from role claims")]
    public async Task GetUserPermissionsAsync_ShouldReadRoleClaims()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        AddRoleClaim(roleId, ReadPermission);
        _dbContext.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().Contain(ReadPermission);
    }

    [Fact(DisplayName = "PermissionStore: should read direct user claims")]
    public async Task GetUserPermissionsAsync_ShouldReadDirectClaims()
    {
        Guid userId = Guid.NewGuid();
        AddUserClaim(userId, CreatePermission);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().Contain(CreatePermission);
    }

    [Fact(DisplayName = "PermissionStore: should merge role and direct claims into a case-insensitive set")]
    public async Task GetUserPermissionsAsync_ShouldMergeDistinctClaims()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        AddRoleClaim(roleId, ReadPermission);
        AddUserClaim(userId, DeletePermission);
        AddUserClaim(userId, ReadPermission.ToUpperInvariant());
        _dbContext.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(ReadPermission);
        result.Value.Should().Contain(DeletePermission);
    }

    [Fact(DisplayName = "PermissionStore: should ignore claims whose type is not permission")]
    public async Task GetUserPermissionsAsync_ShouldIgnoreNonPermissionClaims()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        _dbContext.Set<RoleClaim>().Add(new RoleClaim
        {
            RoleId = roleId,
            ClaimType = "other",
            ClaimValue = ReadPermission,
        });
        _dbContext.Set<UserClaim>().Add(new UserClaim
        {
            UserId = userId,
            ClaimType = "other",
            ClaimValue = CreatePermission,
        });
        _dbContext.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetUserPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionStore: should read a role's permission claims only")]
    public async Task GetRolePermissionsAsync_ShouldReadPermissionClaimsOnly()
    {
        Guid roleId = Guid.NewGuid();
        AddRoleClaim(roleId, ReadPermission);
        _dbContext.Set<RoleClaim>().Add(new RoleClaim
        {
            RoleId = roleId,
            ClaimType = "other",
            ClaimValue = CreatePermission,
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetRolePermissionsAsync(
            roleId,
            TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle().Which.Should().Be(ReadPermission);
    }

    [Fact(DisplayName = "PermissionStore: should return distinct role IDs for a user")]
    public async Task GetUserRoleIdsAsync_ShouldReturnDistinctRoleIds()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId1 = Guid.NewGuid();
        Guid roleId2 = Guid.NewGuid();

        _dbContext.Set<UserRole>().AddRange(
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<Guid>> result = await _sut.GetUserRoleIdsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().BeEquivalentTo([roleId1, roleId2]);
    }

    [Fact(DisplayName = "PermissionStore: should return only direct user claims, excluding role claims")]
    public async Task GetUserDirectPermissionsAsync_ShouldExcludeRoleClaims()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        AddRoleClaim(roleId, ReadPermission);
        AddUserClaim(userId, CreatePermission);
        _dbContext.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetUserDirectPermissionsAsync(
            userId,
            TestContext.Current.CancellationToken);

        result.Value.Should().ContainSingle().Which.Should().Be(CreatePermission);
    }

    [Fact(DisplayName = "PermissionStore: should batch add and remove role permissions")]
    public async Task AddAndRemoveRolePermissionsAsync_ShouldPersistChanges()
    {
        Guid roleId = Guid.NewGuid();

        Result addResult = await _sut.AddRolePermissionsAsync(
            roleId,
            [ReadPermission, DeletePermission],
            TestContext.Current.CancellationToken);

        addResult.IsSuccess.Should().BeTrue();
        List<RoleClaim> stored = await RoleClaims(roleId);
        stored.Should().HaveCount(2);
        stored.Should().OnlyContain(claim =>
            claim.ClaimType == PermissionMetadataConstant.ClaimType);

        Result removeResult = await _sut.RemoveRolePermissionsAsync(
            roleId,
            [ReadPermission],
            TestContext.Current.CancellationToken);

        removeResult.IsSuccess.Should().BeTrue();
        stored = await RoleClaims(roleId);
        stored.Should().ContainSingle().Which.ClaimValue.Should().Be(DeletePermission);
    }

    [Fact(DisplayName = "PermissionStore: should batch add and remove direct user permissions")]
    public async Task AddAndRemoveUserPermissionsAsync_ShouldPersistChanges()
    {
        Guid userId = Guid.NewGuid();

        Result addResult = await _sut.AddUserDirectPermissionsAsync(
            userId,
            [CreatePermission, DeletePermission],
            TestContext.Current.CancellationToken);

        addResult.IsSuccess.Should().BeTrue();
        List<UserClaim> stored = await UserClaims(userId);
        stored.Should().HaveCount(2);

        Result removeResult = await _sut.RemoveUserDirectPermissionsAsync(
            userId,
            [CreatePermission],
            TestContext.Current.CancellationToken);

        removeResult.IsSuccess.Should().BeTrue();
        stored = await UserClaims(userId);
        stored.Should().ContainSingle().Which.ClaimValue.Should().Be(DeletePermission);
    }

    [Fact(DisplayName = "PermissionStore: adding a permission twice should be idempotent")]
    public async Task AddRolePermissionsAsync_ShouldBeIdempotent()
    {
        Guid roleId = Guid.NewGuid();

        await _sut.AddRolePermissionsAsync(roleId, [ReadPermission], TestContext.Current.CancellationToken);
        await _sut.AddRolePermissionsAsync(
            roleId,
            [ReadPermission.ToUpperInvariant(), ReadPermission],
            TestContext.Current.CancellationToken);

        List<RoleClaim> stored = await RoleClaims(roleId);
        stored.Should().ContainSingle().Which.ClaimValue.Should().Be(ReadPermission);
    }

    [Fact(DisplayName = "PermissionStore: adding a direct permission twice should be idempotent")]
    public async Task AddUserDirectPermissionsAsync_ShouldBeIdempotent()
    {
        Guid userId = Guid.NewGuid();

        await _sut.AddUserDirectPermissionsAsync(userId, [CreatePermission], TestContext.Current.CancellationToken);
        await _sut.AddUserDirectPermissionsAsync(userId, [CreatePermission], TestContext.Current.CancellationToken);

        List<UserClaim> stored = await UserClaims(userId);
        stored.Should().ContainSingle();
    }

    [Fact(DisplayName = "PermissionStore: writes should normalize identifiers and skip blank entries")]
    public async Task AddRolePermissionsAsync_ShouldNormalizeIdentifiers()
    {
        Guid roleId = Guid.NewGuid();

        Result result = await _sut.AddRolePermissionsAsync(
            roleId,
            ["  Store.Catalog.Product.Read  ", "   "],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        List<RoleClaim> stored = await RoleClaims(roleId);
        stored.Should().ContainSingle().Which.ClaimValue.Should().Be(ReadPermission);
    }

    [Fact(DisplayName = "PermissionStore: writes should reject a malformed identifier")]
    public async Task AddRolePermissionsAsync_ShouldRejectMalformedIdentifier()
    {
        Guid roleId = Guid.NewGuid();

        Result result = await _sut.AddRolePermissionsAsync(
            roleId,
            ["not-a-permission"],
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionStore.Identifier.Invalid");
        (await RoleClaims(roleId)).Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionStore: removals should reject a malformed identifier")]
    public async Task RemoveUserDirectPermissionsAsync_ShouldRejectMalformedIdentifier()
    {
        Result result = await _sut.RemoveUserDirectPermissionsAsync(
            Guid.NewGuid(),
            ["not-a-permission"],
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionStore.Identifier.Invalid");
    }

    [Fact(DisplayName = "PermissionStore: writes should reject a batch over the limit")]
    public async Task AddRolePermissionsAsync_ShouldRejectOversizedBatch()
    {
        string[] permissions = [.. Enumerable
            .Range(0, PermissionStoreConstant.Constraints.MaxPermissionsPerBatch + 1)
            .Select(index => $"store.catalog.product{index:D5}.read")];

        Result result = await _sut.AddRolePermissionsAsync(
            Guid.NewGuid(),
            permissions,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("PermissionStore.Batch.TooLarge");
    }

    [Fact(DisplayName = "PermissionStore: empty writes should succeed without touching the database")]
    public async Task Writes_WithEmptyCollections_ShouldSucceed()
    {
        Guid id = Guid.NewGuid();

        (await _sut.AddRolePermissionsAsync(id, [], TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();
        (await _sut.RemoveRolePermissionsAsync(id, [], TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();
        (await _sut.AddUserDirectPermissionsAsync(id, [], TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();
        (await _sut.RemoveUserDirectPermissionsAsync(id, [], TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionStore: removing a missing permission should succeed")]
    public async Task RemoveRolePermissionsAsync_WhenMissing_ShouldSucceed()
    {
        Result result = await _sut.RemoveRolePermissionsAsync(
            Guid.NewGuid(),
            [ReadPermission],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "PermissionStore: removing a permission should leave other claim types intact")]
    public async Task RemoveRolePermissionsAsync_ShouldIgnoreNonPermissionClaims()
    {
        Guid roleId = Guid.NewGuid();
        _dbContext.Set<RoleClaim>().Add(new RoleClaim
        {
            RoleId = roleId,
            ClaimType = "other",
            ClaimValue = ReadPermission,
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _sut.RemoveRolePermissionsAsync(roleId, [ReadPermission], TestContext.Current.CancellationToken);

        (await _dbContext.Set<RoleClaim>().ToListAsync(TestContext.Current.CancellationToken))
            .Should().ContainSingle(claim =>
                claim.RoleId == roleId &&
                claim.ClaimType == "other");
    }

    [Fact(DisplayName = "PermissionStore: should load distinct identifiers across both claim tables")]
    public async Task GetAllPermissionIdentifiersAsync_ShouldDeduplicateAcrossTables()
    {
        AddRoleClaim(Guid.NewGuid(), ReadPermission);
        AddUserClaim(Guid.NewGuid(), ReadPermission);
        AddUserClaim(Guid.NewGuid(), CreatePermission);
        _dbContext.Set<RoleClaim>().Add(new RoleClaim
        {
            RoleId = Guid.NewGuid(),
            ClaimType = "other",
            ClaimValue = DeletePermission,
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<HashSet<string>> result = await _sut.GetAllPermissionIdentifiersAsync(
            TestContext.Current.CancellationToken);

        result.Value.Should().BeEquivalentTo([ReadPermission, CreatePermission]);
    }

    [Fact(DisplayName = "PermissionStore: should return an empty set when there are no identifiers")]
    public async Task GetAllPermissionIdentifiersAsync_ShouldReturnEmpty_WhenEmpty()
    {
        Result<HashSet<string>> result = await _sut.GetAllPermissionIdentifiersAsync(
            TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionStore: reads should return specific failures when persistence fails")]
    public async Task Reads_WhenPersistenceFails_ShouldReturnSpecificFailures()
    {
        Guid id = Guid.NewGuid();
        _dbContext.Dispose();

        Result<HashSet<string>> user = await _sut.GetUserPermissionsAsync(
            id, TestContext.Current.CancellationToken);
        Result<HashSet<string>> role = await _sut.GetRolePermissionsAsync(
            id, TestContext.Current.CancellationToken);
        Result<HashSet<string>> direct = await _sut.GetUserDirectPermissionsAsync(
            id, TestContext.Current.CancellationToken);
        Result<HashSet<Guid>> roleIds = await _sut.GetUserRoleIdsAsync(
            id, TestContext.Current.CancellationToken);
        Result<HashSet<string>> all = await _sut.GetAllPermissionIdentifiersAsync(
            TestContext.Current.CancellationToken);

        user.Errors[0].Code.Should().Be("PermissionStore.User.Permissions.ReadFailed");
        role.Errors[0].Code.Should().Be("PermissionStore.Role.Permissions.ReadFailed");
        direct.Errors[0].Code.Should().Be("PermissionStore.User.DirectPermissions.ReadFailed");
        roleIds.Errors[0].Code.Should().Be("PermissionStore.User.RoleIds.ReadFailed");
        all.Errors[0].Code.Should().Be("PermissionStore.AllIdentifiers.ReadFailed");
    }

    [Fact(DisplayName = "PermissionStore: writes should fail when persistence fails")]
    public async Task Writes_WhenPersistenceFails_ShouldReturnFailure()
    {
        Guid id = Guid.NewGuid();
        _dbContext.Dispose();

        Result addRole = await _sut.AddRolePermissionsAsync(
            id, [ReadPermission], TestContext.Current.CancellationToken);
        Result removeRole = await _sut.RemoveRolePermissionsAsync(
            id, [ReadPermission], TestContext.Current.CancellationToken);
        Result addUser = await _sut.AddUserDirectPermissionsAsync(
            id, [ReadPermission], TestContext.Current.CancellationToken);
        Result removeUser = await _sut.RemoveUserDirectPermissionsAsync(
            id, [ReadPermission], TestContext.Current.CancellationToken);

        addRole.Errors[0].Code.Should().Be("PermissionStore.Role.Permissions.Add.Failed");
        addUser.Errors[0].Code.Should().Be("PermissionStore.User.Permissions.Add.Failed");
        removeRole.Errors[0].Code.Should().Be("PermissionStore.Role.Permissions.Remove.Failed");
        removeUser.Errors[0].Code.Should().Be("PermissionStore.User.Permissions.Remove.Failed");
    }

    private void AddRoleClaim(Guid roleId, string value) =>
        _dbContext.Set<RoleClaim>().Add(new RoleClaim
        {
            RoleId = roleId,
            ClaimType = PermissionMetadataConstant.ClaimType,
            ClaimValue = value,
        });

    private void AddUserClaim(Guid userId, string value) =>
        _dbContext.Set<UserClaim>().Add(new UserClaim
        {
            UserId = userId,
            ClaimType = PermissionMetadataConstant.ClaimType,
            ClaimValue = value,
        });

    private Task<List<RoleClaim>> RoleClaims(Guid roleId) =>
        _dbContext.Set<RoleClaim>()
            .Where(claim => claim.RoleId == roleId)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<UserClaim>> UserClaims(Guid userId) =>
        _dbContext.Set<UserClaim>()
            .Where(claim => claim.UserId == userId)
            .ToListAsync(TestContext.Current.CancellationToken);
}
