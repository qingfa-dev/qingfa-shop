using Infrastructure.Security.Identity.Domain.Roles;

namespace Infrastructure.Unit.Tests.Security.Identity.Domain.Roles;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Roles")]
public sealed class RoleConstantTests
{
    [Fact(DisplayName = "RoleConstant: Admin should have the identity management permissions")]
    public void Admin_ShouldHaveIdentityManagementPermissions()
    {
        RoleConstant.RolePermissions.Admin.Should().HaveCountGreaterThanOrEqualTo(5);
        RoleConstant.RolePermissions.Admin.Select(permission => permission.Identifier)
            .Should().Contain("identity.users.all.manage");
    }

    [Fact(DisplayName = "RoleConstant: Manager and User should be permission subsets of Admin")]
    public void ManagerAndUser_ShouldBeSubsetsOfAdmin()
    {
        string[] admin = [.. RoleConstant.RolePermissions.Admin.Select(permission => permission.Identifier)];

        RoleConstant.RolePermissions.Manager.Should().NotBeEmpty();
        RoleConstant.RolePermissions.User.Should().NotBeEmpty();
        RoleConstant.RolePermissions.Manager.Select(permission => permission.Identifier)
            .Should().BeSubsetOf(admin);
        RoleConstant.RolePermissions.User.Select(permission => permission.Identifier)
            .Should().BeSubsetOf(admin);
    }

    [Fact(DisplayName = "RoleConstant: defaults and query allowlists should be complete")]
    public void DefaultsAndQuery_ShouldBeComplete()
    {
        RoleConstant.Defaults.All.Should().BeEquivalentTo(
            [RoleConstant.Defaults.Admin, RoleConstant.Defaults.Manager, RoleConstant.Defaults.User]);
        RoleConstant.Query.AllowedSearchFields.Should().NotBeEmpty();
        RoleConstant.Query.AllowedSortFields.Should().NotBeEmpty();
        RoleConstant.Query.AllowedFilterFields.Should().NotBeEmpty();
        RoleConstant.Constraints.Name.MaxLength.Should().BePositive();
        RoleConstant.Constraints.Description.MaxLength.Should().BePositive();
    }
}
