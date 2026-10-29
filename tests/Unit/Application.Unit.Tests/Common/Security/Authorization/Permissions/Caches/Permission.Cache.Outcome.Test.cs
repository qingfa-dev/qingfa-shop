using Application.Common.Caching;
using Application.Common.Security.Authorization.Permissions.Caches;

namespace Application.Unit.Tests.Common.Security.Authorization.Permissions.Caches;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionCache")]
public sealed class PermissionCacheOutcomeTests
{
    [Fact(DisplayName = "UserIdRequired should be a caller-contract error")]
    public void UserIdRequired_ShouldBeBadRequest()
    {
        Error error = PermissionCacheOutcome.Failure.UserIdRequired;

        error.Code.Should().Be("PermissionCache.UserId.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "RoleIdRequired should be a caller-contract error")]
    public void RoleIdRequired_ShouldBeBadRequest()
    {
        Error error = PermissionCacheOutcome.Failure.RoleIdRequired;

        error.Code.Should().Be("PermissionCache.RoleId.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "PermissionsRequired should be a caller-contract error")]
    public void PermissionsRequired_ShouldBeBadRequest()
    {
        Error error = PermissionCacheOutcome.Failure.PermissionsRequired;

        error.Code.Should().Be("PermissionCache.Permissions.Required");
        error.Status.Should().Be(400);
    }

    [Fact(DisplayName = "UserReadFailed should be an availability error containing the user and cause")]
    public void UserReadFailed_ShouldBeServiceUnavailable()
    {
        Guid userId = Guid.NewGuid();
        Error cause = CachingOutcome.Failure.ReadFailed("perm:user:1");

        Error error = PermissionCacheOutcome.Failure.UserReadFailed(userId, [cause]);

        error.Code.Should().Be("PermissionCache.User.ReadFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(userId.ToString());
        error.Message.Should().Contain(cause.Code);
    }

    [Fact(DisplayName = "RoleReadFailed should be an availability error containing the role and cause")]
    public void RoleReadFailed_ShouldBeServiceUnavailable()
    {
        Guid roleId = Guid.NewGuid();
        Error cause = CachingOutcome.Failure.ReadFailed("perm:role:1");

        Error error = PermissionCacheOutcome.Failure.RoleReadFailed(roleId, [cause]);

        error.Code.Should().Be("PermissionCache.Role.ReadFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(roleId.ToString());
        error.Message.Should().Contain(cause.Code);
    }

    [Fact(DisplayName = "UserWriteFailed should be an availability error")]
    public void UserWriteFailed_ShouldBeServiceUnavailable()
    {
        Guid userId = Guid.NewGuid();

        Error error = PermissionCacheOutcome.Failure.UserWriteFailed(
            userId,
            [CachingOutcome.Failure.WriteFailed("key")]);

        error.Code.Should().Be("PermissionCache.User.WriteFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(userId.ToString());
    }

    [Fact(DisplayName = "RoleWriteFailed should be an availability error")]
    public void RoleWriteFailed_ShouldBeServiceUnavailable()
    {
        Guid roleId = Guid.NewGuid();

        Error error = PermissionCacheOutcome.Failure.RoleWriteFailed(
            roleId,
            [CachingOutcome.Failure.WriteFailed("key")]);

        error.Code.Should().Be("PermissionCache.Role.WriteFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(roleId.ToString());
    }

    [Fact(DisplayName = "UserInvalidationFailed should be an availability error")]
    public void UserInvalidationFailed_ShouldBeServiceUnavailable()
    {
        Guid userId = Guid.NewGuid();

        Error error = PermissionCacheOutcome.Failure.UserInvalidationFailed(
            userId,
            [CachingOutcome.Failure.RemoveFailed("key")]);

        error.Code.Should().Be("PermissionCache.User.InvalidationFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(userId.ToString());
    }

    [Fact(DisplayName = "RoleInvalidationFailed should be an availability error")]
    public void RoleInvalidationFailed_ShouldBeServiceUnavailable()
    {
        Guid roleId = Guid.NewGuid();

        Error error = PermissionCacheOutcome.Failure.RoleInvalidationFailed(
            roleId,
            [CachingOutcome.Failure.RemoveFailed("key")]);

        error.Code.Should().Be("PermissionCache.Role.InvalidationFailed");
        error.Status.Should().Be(503);
        error.Message.Should().Contain(roleId.ToString());
    }

    [Fact(DisplayName = "AllInvalidationFailed should be an availability error")]
    public void AllInvalidationFailed_ShouldBeServiceUnavailable()
    {
        Error error = PermissionCacheOutcome.Failure.AllInvalidationFailed(
            [CachingOutcome.Failure.EvictFailed(tag: null)]);

        error.Code.Should().Be("PermissionCache.All.InvalidationFailed");
        error.Status.Should().Be(503);
    }

    [Fact(DisplayName = "Outcome failures should handle an empty cause list")]
    public void Failures_WithNoCauses_ShouldStillExplainTheFailure()
    {
        Error error = PermissionCacheOutcome.Failure.UserReadFailed(Guid.NewGuid(), []);

        error.Message.Should().Contain("No cause was reported.");
    }
}
