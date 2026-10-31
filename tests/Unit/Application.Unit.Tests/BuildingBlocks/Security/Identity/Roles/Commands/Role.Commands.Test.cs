using Application.BuildingBlocks.Security.Identity.Roles.Commands;
using Application.BuildingBlocks.Security.Identity.Roles.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Roles.Commands;

/// <summary>Unit tests for the role command handlers.</summary>
public sealed class RoleCommandsTest
{
    [Fact(DisplayName = "Create should map the request and return 201")]
    public async Task Create_ShouldMapRequestAndReturnCreated()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.CreateAsync(
                It.Is<CreateRoleRequestModel>(spec =>
                    spec.Name == "Editor" &&
                    spec.Description == "Content editor" &&
                    spec.Permissions.Count == 1 &&
                    spec.Permissions[0] == "identity.roles.all.read"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));

        Result<RoleModel> result = await new CreateRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new CreateRole.UseCase(new CreateRole.Request
                {
                    Name = "Editor",
                    Description = "Content editor",
                    Permissions = ["identity.roles.all.read"],
                }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.Id.Should().Be(RoleFixture.RoleId);
    }

    [Fact(DisplayName = "Create should preserve a service failure")]
    public async Task Create_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.CreateAsync(
                It.IsAny<CreateRoleRequestModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Fail(RoleFixture.Failure));

        Result<RoleModel> result = await new CreateRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new CreateRole.UseCase(new CreateRole.Request { Name = "Editor" }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Role.Test.NotFound");
    }

    [Fact(DisplayName = "Create should forward an empty request without pre-validating")]
    public async Task Create_WhenRequestIsEmpty_ShouldReachTheService()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.CreateAsync(
                It.IsAny<CreateRoleRequestModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));

        Result<RoleModel> result = await new CreateRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new CreateRole.UseCase(new CreateRole.Request()),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        fixture.Roles.Verify(
            service => service.CreateAsync(
                It.Is<CreateRoleRequestModel>(spec =>
                    spec.Name == string.Empty &&
                    spec.Description == null &&
                    spec.Permissions.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Update should map the route id and request")]
    public async Task Update_ShouldMapRouteIdAndRequest()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.UpdateAsync(
                RoleFixture.RoleId,
                It.Is<UpdateRoleRequestModel>(spec =>
                    spec.Name == "Supervisor" &&
                    spec.Description == "Supervision"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));

        Result<RoleModel> result = await new UpdateRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new UpdateRole.UseCase(
                    RoleFixture.RoleId,
                    new UpdateRole.Request
                    {
                        Name = "Supervisor",
                        Description = "Supervision",
                    }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Update should preserve a missing role")]
    public async Task Update_WhenRoleMissing_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.UpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<UpdateRoleRequestModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Fail(RoleFixture.Failure));

        Result<RoleModel> result = await new UpdateRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new UpdateRole.UseCase(RoleFixture.RoleId, new UpdateRole.Request()),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Delete should return no content")]
    public async Task Delete_ShouldReturnNoContent()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.DeleteAsync(
                RoleFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await new DeleteRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new DeleteRole.UseCase(RoleFixture.RoleId),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Delete should preserve a missing role")]
    public async Task Delete_WhenRoleMissing_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.DeleteAsync(
                RoleFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(RoleFixture.Failure));

        Result result = await new DeleteRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new DeleteRole.UseCase(RoleFixture.RoleId),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Permission commands should delegate to the service")]
    public async Task PermissionCommands_ShouldDelegate()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.AssignPermissionAsync(
                RoleFixture.RoleId,
                "identity.roles.all.read",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));
        fixture.Roles
            .Setup(service => service.RemovePermissionAsync(
                RoleFixture.RoleId,
                "identity.roles.all.read",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Ok(RoleFixture.Role));

        Result<RoleModel> assigned = await new AssignPermissionToRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new AssignPermissionToRole.UseCase(new AssignPermissionToRole.Request
                {
                    RoleId = RoleFixture.RoleId,
                    Permission = "identity.roles.all.read",
                }),
                CancellationToken.None);
        Result<RoleModel> removed = await new RemovePermissionFromRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new RemovePermissionFromRole.UseCase(new RemovePermissionFromRole.Request
                {
                    RoleId = RoleFixture.RoleId,
                    Permission = "identity.roles.all.read",
                }),
                CancellationToken.None);

        assigned.IsSuccess.Should().BeTrue();
        removed.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Permission commands should preserve a missing role")]
    public async Task PermissionCommands_WhenRoleMissing_ShouldPreserveFailure()
    {
        var fixture = new RoleFixture();
        fixture.Roles
            .Setup(service => service.AssignPermissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Fail(RoleFixture.Failure));
        fixture.Roles
            .Setup(service => service.RemovePermissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RoleModel>.Fail(RoleFixture.Failure));

        Result<RoleModel> assigned = await new AssignPermissionToRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new AssignPermissionToRole.UseCase(new AssignPermissionToRole.Request
                {
                    RoleId = RoleFixture.RoleId,
                    Permission = "identity.roles.all.read",
                }),
                CancellationToken.None);
        Result<RoleModel> removed = await new RemovePermissionFromRole.UseCaseHandler(fixture.Roles.Object)
            .Handle(
                new RemovePermissionFromRole.UseCase(new RemovePermissionFromRole.Request
                {
                    RoleId = RoleFixture.RoleId,
                    Permission = "identity.roles.all.read",
                }),
                CancellationToken.None);

        assigned.IsFailure.Should().BeTrue();
        removed.IsFailure.Should().BeTrue();
    }
}
