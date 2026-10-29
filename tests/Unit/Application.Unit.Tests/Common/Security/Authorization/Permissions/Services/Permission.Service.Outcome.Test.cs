using Application.Common.Security.Authorization.Permissions.Caches;
using Application.Common.Security.Authorization.Permissions.Services;

namespace Application.Unit.Tests.Common.Security.Authorization.Permissions.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionService")]
public sealed class PermissionServiceOutcomeTests
{
    [Fact(DisplayName = "UserIdRequired should be a caller-contract error")]
    public void UserIdRequired_ShouldBeBadRequest()
    {
        Error error = PermissionServiceOutcome.Failure.UserIdRequired;

        error.Code.Should().Be("PermissionService.UserId.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "RoleIdRequired should be a caller-contract error")]
    public void RoleIdRequired_ShouldBeBadRequest()
    {
        Error error = PermissionServiceOutcome.Failure.RoleIdRequired;

        error.Code.Should().Be("PermissionService.RoleId.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "PermissionsRequired should be a caller-contract error")]
    public void PermissionsRequired_ShouldBeBadRequest()
    {
        Error error = PermissionServiceOutcome.Failure.PermissionsRequired;

        error.Code.Should().Be("PermissionService.Permissions.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "RoleInvalidationFailed should be an availability error with the role and cause")]
    public void RoleInvalidationFailed_ShouldBeServiceUnavailable()
    {
        Guid roleId = Guid.NewGuid();
        Error cause = PermissionCacheOutcome.Failure.RoleInvalidationFailed(roleId, []);

        Error error = PermissionServiceOutcome.Failure.RoleInvalidationFailed(roleId, [cause]);

        error.Code.Should().Be("PermissionService.Role.InvalidationFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(roleId.ToString());
        error.Message.Should().Contain(cause.Code);
    }

    [Fact(DisplayName = "UserInvalidationFailed should be an availability error with the user and cause")]
    public void UserInvalidationFailed_ShouldBeServiceUnavailable()
    {
        Guid userId = Guid.NewGuid();
        Error cause = PermissionCacheOutcome.Failure.UserInvalidationFailed(userId, []);

        Error error = PermissionServiceOutcome.Failure.UserInvalidationFailed(userId, [cause]);

        error.Code.Should().Be("PermissionService.User.InvalidationFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(userId.ToString());
        error.Message.Should().Contain(cause.Code);
    }

    [Fact(DisplayName = "Invalidation failures should explain an empty cause list")]
    public void InvalidationFailures_WithNoCauses_ShouldStillExplainTheFailure()
    {
        Error error = PermissionServiceOutcome.Failure.UserInvalidationFailed(Guid.NewGuid(), []);

        error.Message.Should().Contain("No cause was reported.");
    }
}
