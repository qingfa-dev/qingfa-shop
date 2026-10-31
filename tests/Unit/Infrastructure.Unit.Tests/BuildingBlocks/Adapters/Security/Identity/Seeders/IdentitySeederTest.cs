using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.Modules.PermissionCatalog;
using Application.Modules.Todos.Permissions;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles.Claims;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Seeders;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Seeders;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Seeding")]
public sealed class IdentitySeederTest
{
    private static readonly IPermissionCatalog s_catalog = new ApplicationPermissionCatalog(
        [new PlatformPermissionCatalog(), new TodoPermissionCatalog()]);

    private static RoleSeeder CreateRoleSeeder(ApplicationDbContext db) =>
        new(db, s_catalog);

    [Fact(DisplayName = "RoleSeeder should create the default roles and claims")]
    public async Task RoleSeeder_ShouldCreateDefaultRoles()
    {
        await using ApplicationDbContext db = CreateContext();
        RoleSeeder seeder = CreateRoleSeeder(db);

        Result result = await seeder.ExecuteAsync(Mock.Of<IInitializationContext>());

        result.IsSuccess.Should().BeTrue();
        db.Set<Role>().Should().HaveCount(3);
        db.Set<Role>().Select(role => role.Name).Should().BeEquivalentTo(
            RoleConstant.Defaults.All);
        db.Set<Role>().Should().OnlyContain(role => role.IsSystem);
        db.Set<Role>().Should().OnlyContain(role => role.CreatedBy == "System");
        db.Set<RoleClaim>().Should().HaveCount(
            s_catalog.Admin.Count +
            s_catalog.Manager.Count +
            s_catalog.User.Count);

        Role admin = db.Set<Role>().Single(role => role.Name == RoleConstant.Defaults.Admin);
        db.Set<RoleClaim>()
            .Where(claim => claim.RoleId == admin.Id)
            .Should().HaveCount(s_catalog.Admin.Count);
    }

    [Fact(DisplayName = "RoleSeeder should grant aggregate manage permissions only to administrators")]
    public async Task RoleSeeder_ShouldKeepManagePermissionsAdminOnly()
    {
        await using ApplicationDbContext db = CreateContext();
        RoleSeeder seeder = CreateRoleSeeder(db);

        await seeder.ExecuteAsync(Mock.Of<IInitializationContext>());

        string[] manage =
        [
            "todo.todos.item.manage",
            "todo.lists.list.manage",
            "todo.categories.category.manage"
        ];

        string[] adminClaims = ClaimsFor(db, RoleConstant.Defaults.Admin);
        string[] managerClaims = ClaimsFor(db, RoleConstant.Defaults.Manager);
        string[] userClaims = ClaimsFor(db, RoleConstant.Defaults.User);

        adminClaims.Should().HaveCount(23);
        managerClaims.Should().HaveCount(17);
        userClaims.Should().HaveCount(14);

        adminClaims.Should().Contain(manage);
        managerClaims.Should().NotContain(manage);
        userClaims.Should().NotContain(manage);
    }

    [Fact(DisplayName = "RoleSeeder should be idempotent and preview pending work")]
    public async Task RoleSeeder_ShouldBeIdempotent()
    {
        await using ApplicationDbContext db = CreateContext();
        var seeder = CreateRoleSeeder(db);
        var initialization = Mock.Of<IInitializationContext>();

        Result<IReadOnlyList<InitializationOperation>> preview =
            await seeder.PreviewAsync(initialization);
        preview.Value.Should().HaveCount(3);

        await seeder.ExecuteAsync(initialization);
        Result<IReadOnlyList<InitializationOperation>> seededPreview =
            await seeder.PreviewAsync(initialization);
        seededPreview.Value.Should().ContainSingle()
            .Which.Type.Should().Be(InitializationOperationType.DataCheck);

        (await seeder.ExecuteAsync(initialization)).IsSuccess.Should().BeTrue();
        db.Set<Role>().Should().HaveCount(3);
    }

    [Fact(DisplayName = "RoleSeeder should sync newly introduced permissions into existing roles")]
    public async Task RoleSeeder_ShouldAddMissingClaimsToExistingRoles()
    {
        await using ApplicationDbContext db = CreateContext();
        var seeder = CreateRoleSeeder(db);
        var initialization = Mock.Of<IInitializationContext>();

        await seeder.ExecuteAsync(initialization);

        Role admin = await db.Set<Role>().SingleAsync(
            role => role.Name == RoleConstant.Defaults.Admin,
            TestContext.Current.CancellationToken);
        RoleClaim removed = await db.Set<RoleClaim>().FirstAsync(
            claim => claim.RoleId == admin.Id,
            TestContext.Current.CancellationToken);
        db.Set<RoleClaim>().Remove(removed);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<IReadOnlyList<InitializationOperation>> preview =
            await seeder.PreviewAsync(initialization);
        preview.Value.Should().Contain(operation =>
            operation.Type == InitializationOperationType.DataChange);

        Result result = await seeder.ExecuteAsync(initialization);

        result.IsSuccess.Should().BeTrue();
        List<string> values = await db.Set<RoleClaim>()
            .Where(claim => claim.RoleId == admin.Id)
            .Select(claim => claim.ClaimValue!)
            .ToListAsync(TestContext.Current.CancellationToken);
        values.Should().BeEquivalentTo(
            s_catalog.Admin.Select(permission => permission.Identifier));
    }

    [Fact(DisplayName = "UserSeeder should create users linked to default roles")]
    public async Task UserSeeder_ShouldCreateUsersAndLinks()
    {
        await using ApplicationDbContext db = CreateContext();
        var initialization = Mock.Of<IInitializationContext>();
        await CreateRoleSeeder(db).ExecuteAsync(initialization);
        var seeder = new UserSeeder(db);

        Result result = await seeder.ExecuteAsync(initialization);

        result.IsSuccess.Should().BeTrue();
        db.Set<User>().Should().HaveCount(3);
        db.Set<User>().Should().OnlyContain(user => user.EmailConfirmed);
        db.Set<User>().Should().OnlyContain(user => !string.IsNullOrWhiteSpace(user.PasswordHash));
        db.Set<User>().Select(user => user.UserName).Should().BeEquivalentTo(
            "admin", "manager", "user");
        db.Set<UserRole>().Should().HaveCount(3);
    }

    [Fact(DisplayName = "UserSeeder should preview and remain idempotent")]
    public async Task UserSeeder_ShouldPreviewAndRemainIdempotent()
    {
        await using ApplicationDbContext db = CreateContext();
        var initialization = Mock.Of<IInitializationContext>();
        await CreateRoleSeeder(db).ExecuteAsync(initialization);
        var seeder = new UserSeeder(db);

        Result<IReadOnlyList<InitializationOperation>> preview =
            await seeder.PreviewAsync(initialization);
        preview.Value.Should().ContainSingle()
            .Which.Type.Should().Be(InitializationOperationType.DataChange);

        await seeder.ExecuteAsync(initialization);
        Result<IReadOnlyList<InitializationOperation>> seededPreview =
            await seeder.PreviewAsync(initialization);
        seededPreview.Value.Should().ContainSingle()
            .Which.Type.Should().Be(InitializationOperationType.DataCheck);

        (await seeder.ExecuteAsync(initialization)).IsSuccess.Should().BeTrue();
        db.Set<User>().Should().HaveCount(3);
    }

    [Fact(DisplayName = "UserSeeder should fail when roles were not seeded")]
    public async Task UserSeeder_ShouldFailWithoutRoles()
    {
        await using ApplicationDbContext db = CreateContext();
        var seeder = new UserSeeder(db);

        Result result = await seeder.ExecuteAsync(Mock.Of<IInitializationContext>());

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Role.NotFound");
    }

    private static string[] ClaimsFor(ApplicationDbContext db, string roleName)
    {
        Guid roleId = db.Set<Role>().Single(role => role.Name == roleName).Id;
        return
        [
            .. db.Set<RoleClaim>()
                .Where(claim => claim.RoleId == roleId)
                .Select(claim => claim.ClaimValue!)
                .ToList()
        ];
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"IdentitySeed_{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
