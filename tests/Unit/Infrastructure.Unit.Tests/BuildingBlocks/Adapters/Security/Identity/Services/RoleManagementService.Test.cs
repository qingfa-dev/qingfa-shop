using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Pagination;
using Application.BuildingBlocks.Querying.Searching;
using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Permissions.Caches;
using Application.BuildingBlocks.Security.Authorization.Permissions.Services;
using Application.BuildingBlocks.Security.Authorization.Permissions.Store;
using Application.BuildingBlocks.Security.Identity.Abstractions;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Permissions.Services;
using Infrastructure.BuildingBlocks.Adapters.Security.Authorization.Permissions.Store;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles.Claims;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Roles")]
public sealed class RoleManagementServiceTest
{
    private const string ReadPermission = "identity.roles.all.read";

    [Fact(DisplayName = "Search should filter, page, clamp, and load permissions")]
    public async Task Search_ShouldFilterPageClampAndLoadPermissions()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role viewer = await host.CreateRoleAsync("Viewer", "Read only");
        Role editor = await host.CreateRoleAsync("Editor", "Content editor");
        await host.Roles.AssignPermissionAsync(editor.Id, ReadPermission);

        Result<QueryPage<RoleModel>> first = await host.Roles.SearchAsync(
            new QueryParameters { Page = 1, PageSize = 1 });

        first.IsSuccess.Should().BeTrue();
        first.Value.TotalCount.Should().Be(2);
        first.Value.Page.Should().Be(1);
        first.Value.PageSize.Should().Be(1);
        first.Value.Items.Should().ContainSingle();
        first.Value.Items[0].Name.Should().Be("Editor");
        first.Value.Items[0].Description.Should().Be("Content editor");
        first.Value.Items[0].Permissions.Should().Equal(ReadPermission);

        Result<QueryPage<RoleModel>> filtered = await host.Roles.SearchAsync(
            new QueryParameters { Search = "view" });

        filtered.Value.TotalCount.Should().Be(1);
        filtered.Value.Items[0].Id.Should().Be(viewer.Id);

        Result<QueryPage<RoleModel>> descriptionSearch = await host.Roles.SearchAsync(
            new QueryParameters { Search = "content" });

        descriptionSearch.Value.TotalCount.Should().Be(1);
        descriptionSearch.Value.Items[0].Id.Should().Be(editor.Id);

        Result<QueryPage<RoleModel>> clamped = await host.Roles.SearchAsync(
            new QueryParameters { Page = 0, PageSize = 500 });

        clamped.Value.Page.Should().Be(1);
        clamped.Value.PageSize.Should().Be(200);

        Result<QueryPage<RoleModel>> empty = await host.Roles.SearchAsync(
            new QueryParameters { Search = "nobody" });

        empty.Value.TotalCount.Should().Be(0);
        empty.Value.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "Search should reject every unsupported query parameter")]
    public async Task Search_ShouldRejectUnsupportedParameters()
    {
        await using TestHost host = await TestHost.CreateAsync();

        QueryParameters[] unsupported =
        [
            new() { Filter = "name=Admin" },
            new() { OrderBy = "name" },
            new() { Search = "adm", SearchFields = "name" },
            new() { Search = "adm", SearchMode = SearchMode.Contains },
            new() { Search = "adm", SearchTermLogic = SearchTermLogic.Or },
            new() { Search = "adm", SearchCaseInsensitive = true },
            new() { Select = "name" },
            new() { GroupBy = "isSystem" },
            new() { Aggregate = "count(id)" },
        ];

        foreach (QueryParameters parameters in unsupported)
        {
            Result<QueryPage<RoleModel>> result = await host.Roles.SearchAsync(parameters);

            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().ContainSingle(
                error => error.Code == QueryOutcome.Codes.Unsupported);
        }
    }

    [Fact(DisplayName = "Get should return the role or a 404 failure")]
    public async Task Get_ShouldReturnRoleOrNotFound()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role role = await host.CreateRoleAsync("Editor");
        await host.Roles.AssignPermissionAsync(role.Id, ReadPermission);

        Result<RoleModel> found = await host.Roles.GetAsync(role.Id);

        found.IsSuccess.Should().BeTrue();
        found.Value.Id.Should().Be(role.Id);
        found.Value.Name.Should().Be("Editor");
        found.Value.Permissions.Should().Equal(ReadPermission);

        Result<RoleModel> missing = await host.Roles.GetAsync(Guid.NewGuid());

        missing.IsFailure.Should().BeTrue();
        missing.StatusCode.Should().Be(404);
        missing.Errors.Should().ContainSingle(error => error.Code == "Role.NotFound");
    }

    [Fact(DisplayName = "Create should persist the role and its permission set")]
    public async Task Create_ShouldPersistRoleAndPermissions()
    {
        await using TestHost host = await TestHost.CreateAsync();

        Result<RoleModel> created = await host.Roles.CreateAsync(new CreateRoleRequestModel
        {
            Name = "Editor",
            Description = "Content editor",
            Permissions = [ReadPermission],
        });

        created.IsSuccess.Should().BeTrue(
            string.Join("; ", created.Errors.Select(error => $"{error.Code}: {error.Message}")));
        created.Value.Name.Should().Be("Editor");
        created.Value.Description.Should().Be("Content editor");
        created.Value.Permissions.Should().Equal(ReadPermission);

        Role? stored = await host.RoleManager.FindByNameAsync("Editor");
        stored.Should().NotBeNull();
        stored!.CreatedBy.Should().Be("System");
        stored.CreatedAtUtc.Should().NotBe(default);

        host.Cache.Verify(
            cache => cache.InvalidateRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Create should reject duplicates and invalid values")]
    public async Task Create_ShouldRejectDuplicatesAndInvalidValues()
    {
        await using TestHost host = await TestHost.CreateAsync();
        await host.CreateRoleAsync("Editor");

        Result<RoleModel> duplicate = await host.Roles.CreateAsync(
            new CreateRoleRequestModel { Name = "Editor" });

        duplicate.StatusCode.Should().Be(409);
        duplicate.Errors.Should().ContainSingle(error => error.Code == "Role.AlreadyExists");

        Result<RoleModel> emptyName = await host.Roles.CreateAsync(
            new CreateRoleRequestModel { Name = "   " });

        emptyName.Errors.Should().ContainSingle(error => error.Code == "Role.Name.Required");

        Result<RoleModel> longName = await host.Roles.CreateAsync(
            new CreateRoleRequestModel
            {
                Name = new string('a', RoleConstant.Constraints.Name.MaxLength + 1),
            });

        longName.Errors.Should().ContainSingle(error => error.Code == "Role.Name.TooLong");

        Result<RoleModel> longDescription = await host.Roles.CreateAsync(
            new CreateRoleRequestModel
            {
                Name = "Author",
                Description = new string('a', RoleConstant.Constraints.Description.MaxLength + 1),
            });

        longDescription.Errors.Should().ContainSingle(
            error => error.Code == "Role.Description.TooLong");

        Result<RoleModel> invalidPermission = await host.Roles.CreateAsync(
            new CreateRoleRequestModel
            {
                Name = "Auditor",
                Permissions = ["not-a-permission"],
            });

        invalidPermission.Errors.Should().ContainSingle(
            error => error.Code == "PermissionStore.Identifier.Invalid");
    }

    [Fact(DisplayName = "Update should change a mutable role and protect system roles")]
    public async Task Update_ShouldChangeMutableRoleAndProtectSystemRoles()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role role = await host.CreateRoleAsync("Editor", "Content editor");

        Result<RoleModel> updated = await host.Roles.UpdateAsync(
            role.Id,
            new UpdateRoleRequestModel
            {
                Name = "Supervisor",
                Description = "Supervision",
            });

        updated.IsSuccess.Should().BeTrue();
        updated.Value.Name.Should().Be("Supervisor");
        updated.Value.Description.Should().Be("Supervision");

        Role system = await host.CreateRoleAsync("System", "Protected", isSystem: true);
        Result<RoleModel> protectedRole = await host.Roles.UpdateAsync(
            system.Id,
            new UpdateRoleRequestModel { Name = "Renamed" });

        protectedRole.StatusCode.Should().Be(403);
        protectedRole.Errors.Should().ContainSingle(
            error => error.Code == "Role.System.Protected");

        Result<RoleModel> missing = await host.Roles.UpdateAsync(
            Guid.NewGuid(),
            new UpdateRoleRequestModel { Name = "Missing" });

        missing.StatusCode.Should().Be(404);

        Result<RoleModel> invalid = await host.Roles.UpdateAsync(
            role.Id,
            new UpdateRoleRequestModel { Name = "   " });

        invalid.Errors.Should().ContainSingle(error => error.Code == "Role.Name.Required");
    }

    [Fact(DisplayName = "Delete should remove a mutable role and reject restricted roles")]
    public async Task Delete_ShouldRemoveRoleAndRejectRestrictedRoles()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role removable = await host.CreateRoleAsync("Removable");
        Role assigned = await host.CreateRoleAsync("Assigned");
        Role system = await host.CreateRoleAsync("System", "Protected", isSystem: true);

        host.Db.Set<UserRole>().Add(new UserRole
        {
            UserId = Guid.NewGuid(),
            RoleId = assigned.Id,
        });
        await host.Db.SaveChangesAsync();

        Result assignedResult = await host.Roles.DeleteAsync(assigned.Id);

        assignedResult.StatusCode.Should().Be(422);
        assignedResult.Errors.Should().ContainSingle(
            error => error.Code == "Role.CannotDeleteRoleWithUsers");

        Result systemResult = await host.Roles.DeleteAsync(system.Id);

        systemResult.StatusCode.Should().Be(403);
        systemResult.Errors.Should().ContainSingle(
            error => error.Code == "Role.System.Protected");

        Result missing = await host.Roles.DeleteAsync(Guid.NewGuid());

        missing.StatusCode.Should().Be(404);

        Result deleted = await host.Roles.DeleteAsync(removable.Id);

        deleted.IsSuccess.Should().BeTrue();
        (await host.Roles.GetAsync(removable.Id)).StatusCode.Should().Be(404);
        host.Cache.Verify(
            cache => cache.InvalidateRoleAsync(
                removable.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Permission assignment and removal should be idempotent")]
    public async Task PermissionAssignment_ShouldBeIdempotent()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role role = await host.CreateRoleAsync("Editor");

        Result<RoleModel> assigned = await host.Roles.AssignPermissionAsync(
            role.Id,
            ReadPermission);
        Result<RoleModel> assignedAgain = await host.Roles.AssignPermissionAsync(
            role.Id,
            ReadPermission);

        assigned.Value.Permissions.Should().Equal(ReadPermission);
        assignedAgain.Value.Permissions.Should().Equal(ReadPermission);
        host.Db.Set<RoleClaim>()
            .Count(claim =>
                claim.RoleId == role.Id &&
                claim.ClaimType == PermissionMetadataConstant.ClaimType)
            .Should().Be(1);

        Result<RoleModel> removed = await host.Roles.RemovePermissionAsync(
            role.Id,
            ReadPermission);
        Result<RoleModel> removedAgain = await host.Roles.RemovePermissionAsync(
            role.Id,
            ReadPermission);

        removed.Value.Permissions.Should().BeEmpty();
        removedAgain.Value.Permissions.Should().BeEmpty();

        Result<RoleModel> missingAssign = await host.Roles.AssignPermissionAsync(
            Guid.NewGuid(),
            ReadPermission);
        Result<RoleModel> missingRemove = await host.Roles.RemovePermissionAsync(
            Guid.NewGuid(),
            ReadPermission);

        missingAssign.StatusCode.Should().Be(404);
        missingRemove.StatusCode.Should().Be(404);

        Result<RoleModel> invalid = await host.Roles.AssignPermissionAsync(
            role.Id,
            "not-a-permission");
        Result<RoleModel> invalidRemove = await host.Roles.RemovePermissionAsync(
            role.Id,
            "not-a-permission");

        invalid.Errors.Should().ContainSingle(
            error => error.Code == "PermissionStore.Identifier.Invalid");
        invalidRemove.Errors.Should().ContainSingle(
            error => error.Code == "PermissionStore.Identifier.Invalid");
    }

    [Fact(DisplayName = "Create should translate Identity failures")]
    public async Task Create_WhenIdentityFails_ShouldTranslateTheFailure()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Mock<RoleManager<Role>> roleManager = CreateRoleManager();

        roleManager
            .Setup(manager => manager.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Role?)null);
        roleManager
            .Setup(manager => manager.CreateAsync(It.IsAny<Role>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "The role could not be saved.",
            }));

        var service = new RoleManagementService(
            roleManager.Object,
            host.Db,
            Mock.Of<IPermissionService>());

        Result<RoleModel> failed = await service.CreateAsync(
            new CreateRoleRequestModel { Name = "Editor" });

        failed.Errors.Should().ContainSingle(
            error => error.Code == "Role.Operation.Failed");

        roleManager
            .Setup(manager => manager.CreateAsync(It.IsAny<Role>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateRoleName",
                Description = "The role already exists.",
            }));

        Result<RoleModel> duplicate = await service.CreateAsync(
            new CreateRoleRequestModel { Name = "Editor" });

        duplicate.StatusCode.Should().Be(409);
        duplicate.Errors.Should().ContainSingle(
            error => error.Code == "Role.AlreadyExists");
    }

    [Fact(DisplayName = "Update and delete should translate Identity failures")]
    public async Task UpdateAndDelete_WhenIdentityFails_ShouldTranslateTheFailure()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role mutable = await host.CreateRoleAsync("Editor");
        Role removable = await host.CreateRoleAsync("Removable");
        Mock<RoleManager<Role>> roleManager = CreateRoleManager();

        roleManager
            .Setup(manager => manager.FindByIdAsync(mutable.Id.ToString()))
            .ReturnsAsync(mutable);
        roleManager
            .Setup(manager => manager.FindByIdAsync(removable.Id.ToString()))
            .ReturnsAsync(removable);
        roleManager
            .Setup(manager => manager.UpdateAsync(mutable))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "The role could not be updated.",
            }));
        roleManager
            .Setup(manager => manager.DeleteAsync(removable))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "The role could not be deleted.",
            }));

        var service = new RoleManagementService(
            roleManager.Object,
            host.Db,
            Mock.Of<IPermissionService>());

        Result<RoleModel> updated = await service.UpdateAsync(
            mutable.Id,
            new UpdateRoleRequestModel { Name = "Supervisor" });
        Result deleted = await service.DeleteAsync(removable.Id);

        updated.Errors.Should().ContainSingle(
            error => error.Code == "Role.Operation.Failed");
        deleted.Errors.Should().ContainSingle(
            error => error.Code == "Role.Operation.Failed");
    }

    private static Mock<RoleManager<Role>> CreateRoleManager()
    {
        var store = new Mock<IRoleStore<Role>>();
        var normalizer = new Mock<ILookupNormalizer>();
        var logger = new Mock<ILogger<RoleManager<Role>>>();

        return new Mock<RoleManager<Role>>(
            store.Object,
            new List<IRoleValidator<Role>>(),
            normalizer.Object,
            new IdentityErrorDescriber(),
            logger.Object);
    }

    [Fact(DisplayName = "Permission failures should propagate from the permission service")]
    public async Task PermissionOperations_WhenServiceFails_ShouldPropagate()
    {
        await using TestHost host = await TestHost.CreateAsync();
        Role role = await host.CreateRoleAsync("Editor");

        var failing = new Mock<IPermissionService>();
        failing
            .Setup(service => service.AddRolePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected(
                "Test.Permission.AddFailed",
                "The permission could not be added.")));
        failing
            .Setup(service => service.RemoveRolePermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(Error.Unexpected(
                "Test.Permission.RemoveFailed",
                "The permission could not be removed.")));

        var service = new RoleManagementService(
            host.RoleManager,
            host.Db,
            failing.Object);

        Result<RoleModel> assigned = await service.AssignPermissionAsync(
            role.Id,
            ReadPermission);
        Result<RoleModel> removed = await service.RemovePermissionAsync(
            role.Id,
            ReadPermission);
        Result<RoleModel> created = await service.CreateAsync(new CreateRoleRequestModel
        {
            Name = "Auditor",
            Permissions = [ReadPermission],
        });

        assigned.Errors.Should().ContainSingle(
            error => error.Code == "Test.Permission.AddFailed");
        removed.Errors.Should().ContainSingle(
            error => error.Code == "Test.Permission.RemoveFailed");
        created.Errors.Should().ContainSingle(
            error => error.Code == "Test.Permission.AddFailed");
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private TestHost(
            ServiceProvider provider,
            IServiceScope scope,
            IRoleManagementService roles,
            RoleManager<Role> roleManager,
            ApplicationDbContext db,
            Mock<IPermissionCache> cache)
        {
            Provider = provider;
            Scope = scope;
            Roles = roles;
            RoleManager = roleManager;
            Db = db;
            Cache = cache;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public IRoleManagementService Roles { get; }

        public RoleManager<Role> RoleManager { get; }

        public ApplicationDbContext Db { get; }

        public Mock<IPermissionCache> Cache { get; }

        public static async Task<TestHost> CreateAsync()
        {
            var cache = new Mock<IPermissionCache>();
            cache
                .Setup(instance => instance.GetRoleAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PermissionCacheEntry>.Ok(PermissionCacheEntry.Miss));
            cache
                .Setup(instance => instance.SetRoleAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok());
            cache
                .Setup(instance => instance.InvalidateRoleAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Ok());

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"RoleManagementService_{Guid.NewGuid():N}"));
            services.AddIdentityServices();
            services.AddScoped<IPermissionStore, PermissionStoreService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddSingleton(cache.Object);

            ServiceProvider provider = services.BuildServiceProvider();
            IServiceScope scope = provider.CreateScope();

            return new TestHost(
                provider,
                scope,
                scope.ServiceProvider.GetRequiredService<IRoleManagementService>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<Role>>(),
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                cache);
        }

        public async Task<Role> CreateRoleAsync(
            string name,
            string? description = null,
            bool isSystem = false)
        {
            Role role = Role.Create(name, description).Value;
            role.IsSystem = isSystem;

            IdentityResult result = await RoleManager.CreateAsync(role);
            result.Succeeded.Should().BeTrue();

            return role;
        }

        public ValueTask DisposeAsync()
        {
            Scope.Dispose();
            return Provider.DisposeAsync();
        }
    }
}
