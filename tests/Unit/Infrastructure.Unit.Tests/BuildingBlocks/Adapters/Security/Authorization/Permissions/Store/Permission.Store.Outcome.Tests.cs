using Application.BuildingBlocks.Security.Authorization.Permissions.Store;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authorization.Permissions.Store;

[Trait("Category", "Unit")]
[Trait("Module", "Authorization")]
[Trait("Feature", "PermissionStore")]
public sealed class PermissionStoreOutcomeTests
{
    [Fact(DisplayName = "InvalidIdentifier should describe the rejected identifier")]
    public void InvalidIdentifier_ShouldReturnValidationError()
    {
        Error error = PermissionStoreOutcome.Failure.InvalidIdentifier("bad");

        error.Code.Should().Be("PermissionStore.Identifier.Invalid");
        error.Message.Should().Contain("'bad'");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "BatchTooLarge should describe the batch limit")]
    public void BatchTooLarge_ShouldReturnValidationError()
    {
        Error error = PermissionStoreOutcome.Failure.BatchTooLarge(1001);

        error.Code.Should().Be("PermissionStore.Batch.TooLarge");
        error.Message.Should().Contain("1001");
        error.Status.Should().Be(422);
    }

    [Fact(DisplayName = "Read failures should be specific availability errors")]
    public void ReadFailures_ShouldBeSpecificAvailabilityErrors()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();

        PermissionStoreOutcome.Failure.UserPermissionsReadFailed(userId).Code
            .Should().Be("PermissionStore.User.Permissions.ReadFailed");
        PermissionStoreOutcome.Failure.RolePermissionsReadFailed(roleId).Code
            .Should().Be("PermissionStore.Role.Permissions.ReadFailed");
        PermissionStoreOutcome.Failure.UserRoleIdsReadFailed(userId).Code
            .Should().Be("PermissionStore.User.RoleIds.ReadFailed");
        PermissionStoreOutcome.Failure.UserDirectPermissionsReadFailed(userId).Code
            .Should().Be("PermissionStore.User.DirectPermissions.ReadFailed");
        PermissionStoreOutcome.Failure.AllIdentifiersReadFailed().Code
            .Should().Be("PermissionStore.AllIdentifiers.ReadFailed");

        PermissionStoreOutcome.Failure.UserPermissionsReadFailed(userId).Status
            .Should().Be(503);
        PermissionStoreOutcome.Failure.UserPermissionsReadFailed(userId).Message
            .Should().Contain(userId.ToString());
    }

    [Fact(DisplayName = "Write failures should be specific availability errors")]
    public void WriteFailures_ShouldBeSpecificAvailabilityErrors()
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();

        PermissionStoreOutcome.Failure.RolePermissionsAddFailed(roleId).Code
            .Should().Be("PermissionStore.Role.Permissions.Add.Failed");
        PermissionStoreOutcome.Failure.RolePermissionsRemoveFailed(roleId).Code
            .Should().Be("PermissionStore.Role.Permissions.Remove.Failed");
        PermissionStoreOutcome.Failure.UserPermissionsAddFailed(userId).Code
            .Should().Be("PermissionStore.User.Permissions.Add.Failed");
        PermissionStoreOutcome.Failure.UserPermissionsRemoveFailed(userId).Code
            .Should().Be("PermissionStore.User.Permissions.Remove.Failed");

        PermissionStoreOutcome.Failure.RolePermissionsAddFailed(roleId).Status
            .Should().Be(503);
        PermissionStoreOutcome.Failure.RolePermissionsAddFailed(roleId).Message
            .Should().Contain(roleId.ToString());
    }

    [Fact(DisplayName = "Operation failures should include the exception type")]
    public void OperationFailures_ShouldIncludeExceptionType()
    {
        Error error = PermissionStoreOutcome.Failure.UserPermissionsReadFailed(
            Guid.NewGuid(),
            new TimeoutException("timed out"));

        error.Message.Should().Contain("TimeoutException");
    }
}
