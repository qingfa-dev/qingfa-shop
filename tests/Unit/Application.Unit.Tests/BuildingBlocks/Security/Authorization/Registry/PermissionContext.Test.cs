using Application.BuildingBlocks.Security.Authorization.Domain;
using Application.BuildingBlocks.Security.Authorization.Registry;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authorization.Registry;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionContext")]
public sealed class PermissionContextTests
{
    [Fact(DisplayName = "PermissionContext: role permission sets should be non-empty")]
    public void RolePermissionSets_ShouldBeNonEmpty()
    {
        PermissionContext.Admin.Should().NotBeEmpty();
        PermissionContext.Manager.Should().NotBeEmpty();
        PermissionContext.User.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "PermissionContext: manager and user permissions should be admin subsets")]
    public void ManagerAndUser_ShouldBeAdminSubsets()
    {
        string[] admin = [.. PermissionContext.Admin.Select(permission => permission.Identifier)];

        PermissionContext.Manager.Select(permission => permission.Identifier)
            .Should().BeSubsetOf(admin);
        PermissionContext.User.Select(permission => permission.Identifier)
            .Should().BeSubsetOf(admin);
    }

    [Fact(DisplayName = "PermissionContext: every role permission should appear in All")]
    public void RolePermissions_ShouldBeContainedInAll()
    {
        string[] all = [.. PermissionContext.All.Select(permission => permission.Identifier)];

        PermissionContext.Admin.Select(permission => permission.Identifier).Should().BeSubsetOf(all);
        PermissionContext.Manager.Select(permission => permission.Identifier).Should().BeSubsetOf(all);
        PermissionContext.User.Select(permission => permission.Identifier).Should().BeSubsetOf(all);
    }

    [Fact(DisplayName = "PermissionContext: All should expose every distinct canonical permission")]
    public void All_ShouldExposeEveryDistinctCanonicalPermission()
    {
        PermissionContext.All.Should().NotBeEmpty();
        PermissionContext.All.Select(permission => permission.Identifier)
            .Should().OnlyHaveUniqueItems();
        PermissionContext.All.Should().OnlyContain(permission =>
            PermissionMetadataConstant.IsValidIdentifier(permission.Identifier));
        PermissionContext.All.Should().Contain(permission =>
            permission.Identifier == "identity.users.all.manage");
        PermissionContext.All.Should().NotContain(permission =>
            permission.Identifier == "todo.todos.item.read");
    }

    [Fact(DisplayName = "PermissionContext: typed permissions should be registered")]
    public void TypedPermissions_ShouldBeRegistered()
    {
        PermissionContext.IsKnown(PermissionContext.Identity.Users.Manage.Identifier)
            .Should().BeTrue();
        PermissionContext.IsKnown(PermissionContext.Identity.Roles.Read.Identifier)
            .Should().BeTrue();
        PermissionContext.IsKnown(PermissionContext.Identity.Permissions.Manage.Identifier)
            .Should().BeTrue();
        PermissionContext.IsKnown(PermissionContext.Identity.Profile.Update.Identifier)
            .Should().BeTrue();
        PermissionContext.IsKnown("todo.todos.item.delete")
            .Should().BeFalse();
        PermissionContext.IsKnown("todo.lists.list.manage")
            .Should().BeFalse();
        PermissionContext.IsKnown("todo.categories.category.create")
            .Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionContext: IsKnown should recognize registered permissions")]
    public void IsKnown_ShouldRecognizeRegisteredPermissions()
    {
        string identifier = PermissionContext.All[0].Identifier;

        PermissionContext.IsKnown(identifier).Should().BeTrue();
        PermissionContext.IsKnown(identifier.ToUpperInvariant()).Should().BeTrue();
        PermissionContext.IsKnown("unknown.permission").Should().BeFalse();
        PermissionContext.IsKnown(string.Empty).Should().BeFalse();
        PermissionContext.IsKnown("   ").Should().BeFalse();
    }

    [Fact(DisplayName = "PermissionContext: lookups should return permission metadata")]
    public void Lookups_ShouldReturnPermissionMetadata()
    {
        PermissionMetadata reference = PermissionContext.All[0];

        PermissionContext.ByIdentifier(reference.Identifier)!.Identifier
            .Should().Be(reference.Identifier);
        PermissionContext.ByIdentifier("unknown").Should().BeNull();
        PermissionContext.ByIdentifier(string.Empty).Should().BeNull();
        PermissionContext.ByCategory(reference.Category)
            .Should().OnlyContain(permission => permission.Category == reference.Category);
        PermissionContext.ByCategory("unknown").Should().BeEmpty();
    }

    [Fact(DisplayName = "PermissionContext: ByDomain should return only that domain")]
    public void ByDomain_ShouldReturnOnlyThatDomain()
    {
        PermissionContext.ByDomain("identity")
            .Should().OnlyContain(permission => permission.Domain == "identity");
        PermissionContext.ByDomain("IDENTITY").Should().NotBeEmpty();
        PermissionContext.ByDomain("todo").Should().BeEmpty();
        PermissionContext.ByDomain("unknown").Should().BeEmpty();
    }
}
