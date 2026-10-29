using Application.Common.Persistence.Initialization;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Roles.Claims;
using Infrastructure.Security.Identity.Domain.Users;
using Infrastructure.Security.Identity.Domain.Users.Roles;
using Infrastructure.Security.Identity.Seeders;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Unit.Tests.Security.Identity.Seeders;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Seeding")]
public sealed class IdentitySeederTest
{
    [Fact(DisplayName = "RoleSeeder should create the default roles and claims")]
    public async Task RoleSeeder_ShouldCreateDefaultRoles()
    {
        await using ApplicationDbContext db = CreateContext();
        var seeder = new RoleSeeder(db);

        Result result = await seeder.ExecuteAsync(Mock.Of<IInitializationContext>());

        result.IsSuccess.Should().BeTrue();
        db.Set<Role>().Should().HaveCount(3);
        db.Set<Role>().Select(role => role.Name).Should().BeEquivalentTo(
            RoleConstant.Defaults.All);
        db.Set<Role>().Should().OnlyContain(role => role.IsSystem);
        db.Set<Role>().Should().OnlyContain(role => role.CreatedBy == "System");
        db.Set<RoleClaim>().Should().HaveCount(13);
    }

    [Fact(DisplayName = "RoleSeeder should be idempotent and preview pending work")]
    public async Task RoleSeeder_ShouldBeIdempotent()
    {
        await using ApplicationDbContext db = CreateContext();
        var seeder = new RoleSeeder(db);
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

    [Fact(DisplayName = "UserSeeder should create users linked to default roles")]
    public async Task UserSeeder_ShouldCreateUsersAndLinks()
    {
        await using ApplicationDbContext db = CreateContext();
        var initialization = Mock.Of<IInitializationContext>();
        await new RoleSeeder(db).ExecuteAsync(initialization);
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
        await new RoleSeeder(db).ExecuteAsync(initialization);
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"IdentitySeed_{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
