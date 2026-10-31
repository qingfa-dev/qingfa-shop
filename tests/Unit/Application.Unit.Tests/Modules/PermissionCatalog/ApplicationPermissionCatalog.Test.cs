using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;
using Application.Modules.PermissionCatalog;
using Application.Modules.Todos.Permissions;

namespace Application.Unit.Tests.Modules.PermissionCatalog;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "ApplicationPermissionCatalog")]
public sealed class ApplicationPermissionCatalogTests
{
    private static readonly PlatformPermissionCatalog s_platform = new();
    private static readonly TodoPermissionCatalog s_todos = new();

    private static ApplicationPermissionCatalog CreateCatalog() =>
        new([s_platform, s_todos]);

    [Fact(DisplayName = "ApplicationPermissionCatalog: should merge platform and module permissions")]
    public void Constructor_ShouldMergeContributions()
    {
        ApplicationPermissionCatalog catalog = CreateCatalog();

        catalog.All.Should().HaveCount(s_platform.All.Count + s_todos.Permissions.Count);
        catalog.Admin.Should().HaveCount(s_platform.Admin.Count + s_todos.Admin.Count);
        catalog.Manager.Should().HaveCount(s_platform.Manager.Count + s_todos.Manager.Count);
        catalog.User.Should().HaveCount(s_platform.User.Count + s_todos.User.Count);
        catalog.All.Select(permission => permission.Identifier)
            .Should().Contain("identity.users.all.manage")
            .And.Contain("todo.todos.item.read")
            .And.Contain("todo.categories.category.delete");
    }

    [Fact(DisplayName = "ApplicationPermissionCatalog: should preserve contributor ordering")]
    public void Constructor_ShouldPreserveContributorOrdering()
    {
        ApplicationPermissionCatalog catalog = CreateCatalog();

        string[] expected = [.. s_platform.All
            .Concat(s_todos.Permissions)
            .Select(permission => permission.Identifier)];

        catalog.All.Select(permission => permission.Identifier).Should().Equal(expected);
        catalog.All.Take(s_platform.All.Count).Select(permission => permission.Identifier)
            .Should().Equal(s_platform.All.Select(permission => permission.Identifier));
    }

    [Fact(DisplayName = "ApplicationPermissionCatalog: should de-duplicate by identifier case-insensitively")]
    public void Constructor_ShouldDeDuplicateByIdentifier()
    {
        var duplicate = new FakeContributor(
            PermissionContext.Identity.Users.Manage,
            PermissionContext.Identity.Users.Manage);

        var catalog = new ApplicationPermissionCatalog([s_platform, duplicate]);

        catalog.All.Should().HaveCount(s_platform.All.Count);
        catalog.All.Select(permission => permission.Identifier)
            .Should().OnlyHaveUniqueItems();
        catalog.All[0].Name.Should().Be(PermissionContext.Identity.Users.Manage.Name);
    }

    [Fact(DisplayName = "ApplicationPermissionCatalog: manager and user defaults should remain admin subsets")]
    public void RoleDefaults_ShouldBeAdminSubsets()
    {
        ApplicationPermissionCatalog catalog = CreateCatalog();
        string[] admin = [.. catalog.Admin.Select(permission => permission.Identifier)];

        catalog.Manager.Select(permission => permission.Identifier).Should().BeSubsetOf(admin);
        catalog.User.Select(permission => permission.Identifier).Should().BeSubsetOf(admin);
    }

    [Fact(DisplayName = "ApplicationPermissionCatalog: Todo defaults should keep manage admin-only")]
    public void TodoDefaults_ShouldKeepManageAdminOnly()
    {
        ApplicationPermissionCatalog catalog = CreateCatalog();
        string[] manage =
        [
            "todo.todos.item.manage",
            "todo.lists.list.manage",
            "todo.categories.category.manage"
        ];

        catalog.Admin.Select(permission => permission.Identifier)
            .Should().Contain(manage);
        catalog.Manager.Select(permission => permission.Identifier)
            .Should().NotContain(manage);
        catalog.User.Select(permission => permission.Identifier)
            .Should().NotContain(manage);

        catalog.Admin.Where(permission => permission.Domain == "todo")
            .Should().HaveCount(15);
        catalog.Manager.Where(permission => permission.Domain == "todo")
            .Should().HaveCount(12);
        catalog.User.Where(permission => permission.Domain == "todo")
            .Should().HaveCount(12);
    }

    [Fact(DisplayName = "ApplicationPermissionCatalog: null contributions should throw")]
    public void Constructor_WithNullContributors_ShouldThrow()
    {
        IEnumerable<IPermissionCatalogContributor>? contributors = null;

        var act = () => new ApplicationPermissionCatalog(contributors!);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("contributors");
    }

    private sealed class FakeContributor(params PermissionMetadata[] permissions)
        : IPermissionCatalogContributor
    {
        public IReadOnlyList<PermissionMetadata> Permissions { get; } = permissions;

        public IReadOnlyList<PermissionMetadata> Admin => Permissions;

        public IReadOnlyList<PermissionMetadata> Manager => Permissions;

        public IReadOnlyList<PermissionMetadata> User => Permissions;
    }
}
