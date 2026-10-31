using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Roles;

/// <summary>Unit tests for the role projection models.</summary>
public sealed class RoleModelsTest
{
    [Fact(DisplayName = "Role summary should expose the profile and permission count")]
    public void RoleSummary_ShouldExposeProfileAndPermissionCount()
    {
        RoleSummaryModel summary = RoleFixture.Summary;

        summary.Id.Should().Be(RoleFixture.RoleId);
        summary.Name.Should().Be("Admin");
        summary.Description.Should().Be("System administrator");
        summary.PermissionCount.Should().Be(1);
        summary.Should().Be(RoleFixture.Summary);
    }

    [Fact(DisplayName = "Role requests should default to an empty name and permission set")]
    public void RoleRequests_ShouldExposeSafeDefaults()
    {
        var create = new CreateRoleRequestModel();
        var update = new UpdateRoleRequestModel();

        create.Name.Should().BeEmpty();
        create.Description.Should().BeNull();
        create.Permissions.Should().BeEmpty();
        update.Name.Should().BeEmpty();
        update.Description.Should().BeNull();
    }
}
