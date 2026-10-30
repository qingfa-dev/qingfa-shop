using Application.Common.Security.Authorization;
using Application.Features.Identity.Permissions.Models;

namespace Application.Unit.Tests.Features.Identity.Permissions;

/// <summary>Unit tests for the permission check models.</summary>
public sealed class PermissionModelsTest
{
    [Fact(DisplayName = "Check request should default to an empty ALL check")]
    public void CheckRequest_ShouldDefaultToEmptyAllCheck()
    {
        var request = new CheckUserPermissionsRequestModel();

        request.Permissions.Should().BeEmpty();
        request.Mode.Should().Be(PermissionMatchMode.All);
    }

    [Fact(DisplayName = "Check request should accept the supplied values")]
    public void CheckRequest_ShouldAcceptValues()
    {
        var request = new CheckUserPermissionsRequestModel
        {
            Permissions = ["identity.users.all.read"],
            Mode = PermissionMatchMode.Any,
        };

        request.Permissions.Should().Equal("identity.users.all.read");
        request.Mode.Should().Be(PermissionMatchMode.Any);
    }

    [Fact(DisplayName = "Check result should expose the user, requirement, and outcome")]
    public void CheckResult_ShouldExposeValues()
    {
        var result = new PermissionCheckModel
        {
            UserId = PermissionFixture.UserId,
            Permissions = ["identity.users.all.read"],
            Mode = PermissionMatchMode.Any,
            Granted = true,
        };

        result.UserId.Should().Be(PermissionFixture.UserId);
        result.Permissions.Should().Equal("identity.users.all.read");
        result.Mode.Should().Be(PermissionMatchMode.Any);
        result.Granted.Should().BeTrue();
    }
}
