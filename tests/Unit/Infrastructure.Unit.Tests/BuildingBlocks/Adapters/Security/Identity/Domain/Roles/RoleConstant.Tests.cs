using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Roles")]
public sealed class RoleConstantTests
{
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
