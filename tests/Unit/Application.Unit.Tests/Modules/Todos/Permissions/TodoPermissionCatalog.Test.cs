using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.Modules.Todos.Permissions;

namespace Application.Unit.Tests.Modules.Todos.Permissions;

[Trait("Category", "Unit")]
[Trait("Module", "Todos")]
[Trait("Feature", "PermissionCatalog")]
public sealed class TodoPermissionCatalogTests
{
    private static readonly string[] s_manageIdentifiers =
    [
        "todo.todos.item.manage",
        "todo.lists.list.manage",
        "todo.categories.category.manage"
    ];

    private static readonly string[] s_nonManageIdentifiers =
    [
        "todo.todos.item.read",
        "todo.todos.item.create",
        "todo.todos.item.update",
        "todo.todos.item.delete",
        "todo.lists.list.read",
        "todo.lists.list.create",
        "todo.lists.list.update",
        "todo.lists.list.delete",
        "todo.categories.category.read",
        "todo.categories.category.create",
        "todo.categories.category.update",
        "todo.categories.category.delete"
    ];

    [Fact(DisplayName = "TodoPermissionCatalog: admin should own all 15 Todo permissions")]
    public void Admin_ShouldOwnEveryPermission()
    {
        var catalog = new TodoPermissionCatalog();

        catalog.Permissions.Should().HaveCount(15);
        catalog.Admin.Should().HaveCount(15);
        catalog.Admin.Select(permission => permission.Identifier)
            .Should().Contain(s_manageIdentifiers);
    }

    [Fact(DisplayName = "TodoPermissionCatalog: manager should own exactly the 12 non-manage permissions")]
    public void Manager_ShouldExcludeManagePermissions()
    {
        var catalog = new TodoPermissionCatalog();

        catalog.Manager.Should().HaveCount(12);
        catalog.Manager.Select(permission => permission.Identifier)
            .Should().Equal(s_nonManageIdentifiers);
    }

    [Fact(DisplayName = "TodoPermissionCatalog: user should own exactly the 12 non-manage permissions")]
    public void User_ShouldExcludeManagePermissions()
    {
        var catalog = new TodoPermissionCatalog();

        catalog.User.Should().HaveCount(12);
        catalog.User.Select(permission => permission.Identifier)
            .Should().Equal(s_nonManageIdentifiers);
    }

    [Fact(DisplayName = "TodoPermissionCatalog: only admin should contain the manage permissions")]
    public void ManagePermissions_ShouldBeAdminOnly()
    {
        var catalog = new TodoPermissionCatalog();
        string[] admin = [.. catalog.Admin.Select(permission => permission.Identifier)];
        string[] manager = [.. catalog.Manager.Select(permission => permission.Identifier)];
        string[] user = [.. catalog.User.Select(permission => permission.Identifier)];

        admin.Should().Contain(s_manageIdentifiers);
        manager.Should().NotContain(s_manageIdentifiers);
        user.Should().NotContain(s_manageIdentifiers);
    }

    [Fact(DisplayName = "TodoPermissionCatalog: role defaults should be permission subsets of admin")]
    public void RoleDefaults_ShouldBeAdminSubsets()
    {
        var catalog = new TodoPermissionCatalog();
        string[] admin = [.. catalog.Admin.Select(permission => permission.Identifier)];

        catalog.Manager.Select(permission => permission.Identifier).Should().BeSubsetOf(admin);
        catalog.User.Select(permission => permission.Identifier).Should().BeSubsetOf(admin);
    }

    [Fact(DisplayName = "TodoPermissionCatalog: permissions should remain canonical")]
    public void Permissions_ShouldBeCanonical()
    {
        var catalog = new TodoPermissionCatalog();

        catalog.Permissions.Should().OnlyContain(permission =>
            PermissionMetadataConstant.IsValidIdentifier(permission.Identifier));
        catalog.Permissions.Select(permission => permission.Identifier)
            .Should().OnlyHaveUniqueItems();
    }
}
