using Application.Features.Identity.Users.Commands;
using Application.Features.Identity.Users.Models;

namespace Application.Unit.Tests.Features.Identity.Users.Commands;

/// <summary>Unit tests for the admin user command handlers.</summary>
public sealed class UserCommandsTest
{
    private const string Password = "StrongPass1!ab";

    [Fact(DisplayName = "Create should map the request and return 201")]
    public async Task Create_ShouldMapRequestAndReturnCreated()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.CreateAsync(
                It.Is<CreateUserRequestModel>(spec =>
                    spec.UserName == "admin" &&
                    spec.Email == "admin@example.com" &&
                    spec.EmailConfirmed &&
                    spec.RoleIds.Count == 1 &&
                    spec.RoleIds[0] == UserFixture.RoleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> result = await Handler(fixture).Handle(
            new CreateUser.UseCase(new CreateUser.Request
            {
                UserName = "admin",
                Email = "admin@example.com",
                Password = Password,
                FirstName = "Admin",
                EmailConfirmed = true,
                RoleIds = [UserFixture.RoleId],
            }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.Id.Should().Be(UserFixture.UserId);
    }

    [Fact(DisplayName = "Create should preserve a service failure")]
    public async Task Create_WhenServiceFails_ShouldPreserveFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.CreateAsync(
                It.IsAny<CreateUserRequestModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Fail(UserFixture.Failure));

        Result<UserModel> result = await Handler(fixture).Handle(
            new CreateUser.UseCase(CreateRequest()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(
            error => error.Code == "Identity.Test.NotFound");
    }

    [Fact(DisplayName = "Create should forward an empty request without pre-validating")]
    public async Task Create_WhenRequestIsEmpty_ShouldReachTheService()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.CreateAsync(
                It.IsAny<CreateUserRequestModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> result = await Handler(fixture).Handle(
            new CreateUser.UseCase(new CreateUser.Request()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        fixture.Users.Verify(
            service => service.CreateAsync(
                It.Is<CreateUserRequestModel>(spec =>
                    spec.UserName == string.Empty &&
                    spec.Email == string.Empty &&
                    spec.Password == string.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Update should map the partial specification")]
    public async Task Update_ShouldMapSpecification()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.UpdateAsync(
                UserFixture.UserId,
                It.Is<UpdateUserSpec>(spec =>
                    spec.Email == "new@example.com" &&
                    spec.FirstName == "New" &&
                    spec.RoleIds != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> result = await UpdateHandler(fixture).Handle(
            new UpdateUser.UseCase(
                UserFixture.UserId,
                new UpdateUser.Request
                {
                    Email = "new@example.com",
                    FirstName = "New",
                    RoleIds = [UserFixture.RoleId],
                }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Update should propagate a missing user")]
    public async Task Update_WhenUserMissing_ShouldPropagateFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.UpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<UpdateUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Fail(UserFixture.Failure));

        Result<UserModel> result = await UpdateHandler(fixture).Handle(
            new UpdateUser.UseCase(
                UserFixture.UserId,
                new UpdateUser.Request()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Delete should return no content")]
    public async Task Delete_ShouldReturnNoContent()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.DeleteAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await new DeleteUser.UseCaseHandler(fixture.Users.Object).Handle(
            new DeleteUser.UseCase(new DeleteUser.Request { Id = UserFixture.UserId }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Delete should preserve a missing user")]
    public async Task Delete_WhenUserMissing_ShouldPreserveFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.DeleteAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UserFixture.Failure));

        Result result = await new DeleteUser.UseCaseHandler(fixture.Users.Object).Handle(
            new DeleteUser.UseCase(new DeleteUser.Request { Id = UserFixture.UserId }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Activate and deactivate should set the active flag")]
    public async Task ActivateAndDeactivate_ShouldSetFlag()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.SetActiveAsync(
                UserFixture.UserId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));
        fixture.Users
            .Setup(service => service.SetActiveAsync(
                UserFixture.UserId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> activated = await new ActivateUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new ActivateUser.UseCase(new ActivateUser.Request { Id = UserFixture.UserId }),
                CancellationToken.None);
        Result<UserModel> deactivated = await new DeactivateUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new DeactivateUser.UseCase(new DeactivateUser.Request { Id = UserFixture.UserId }),
                CancellationToken.None);

        activated.IsSuccess.Should().BeTrue();
        deactivated.IsSuccess.Should().BeTrue();
        fixture.Users.Verify(
            service => service.SetActiveAsync(
                UserFixture.UserId,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Users.Verify(
            service => service.SetActiveAsync(
                UserFixture.UserId,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Role commands should delegate to the service")]
    public async Task RoleCommands_ShouldDelegate()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.AssignRoleAsync(
                UserFixture.UserId,
                UserFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));
        fixture.Users
            .Setup(service => service.RemoveRoleAsync(
                UserFixture.UserId,
                UserFixture.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> assigned = await new AssignRoleToUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new AssignRoleToUser.UseCase(
                    new AssignRoleToUser.Request { UserId = UserFixture.UserId, RoleId = UserFixture.RoleId }),
                CancellationToken.None);
        Result<UserModel> removed = await new RemoveRoleFromUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new RemoveRoleFromUser.UseCase(
                    new RemoveRoleFromUser.Request { UserId = UserFixture.UserId, RoleId = UserFixture.RoleId }),
                CancellationToken.None);

        assigned.IsSuccess.Should().BeTrue();
        removed.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Role assignment should preserve a missing role")]
    public async Task AssignRole_WhenRoleMissing_ShouldPreserveFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.AssignRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Fail(Error.NotFound(
                "Identity.Role.NotFound",
                "missing")));

        Result<UserModel> result = await new AssignRoleToUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new AssignRoleToUser.UseCase(
                    new AssignRoleToUser.Request { UserId = UserFixture.UserId, RoleId = UserFixture.RoleId }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Password reset should return no content")]
    public async Task ResetPassword_ShouldReturnNoContent()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.ResetPasswordAsync(
                UserFixture.UserId,
                Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await new ResetUserPassword.UseCaseHandler(fixture.Users.Object).Handle(
            new ResetUserPassword.UseCase(
                UserFixture.UserId,
                new ResetUserPassword.Request { NewPassword = Password }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
        fixture.Users.Verify(
            service => service.ResetPasswordAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Confirm email should return no content")]
    public async Task ConfirmEmail_ShouldReturnNoContent()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.ConfirmEmailAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result result = await new ConfirmUserEmail.UseCaseHandler(fixture.Users.Object).Handle(
            new ConfirmUserEmail.UseCase(new ConfirmUserEmail.Request { Id = UserFixture.UserId }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Lock and unlock should set the lock flag")]
    public async Task LockAndUnlock_ShouldSetFlag()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.SetLockedAsync(
                UserFixture.UserId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));
        fixture.Users
            .Setup(service => service.SetLockedAsync(
                UserFixture.UserId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserModel>.Ok(UserFixture.User));

        Result<UserModel> locked = await new LockUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new LockUser.UseCase(new LockUser.Request { Id = UserFixture.UserId }),
                CancellationToken.None);
        Result<UserModel> unlocked = await new UnlockUser.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new UnlockUser.UseCase(new UnlockUser.Request { Id = UserFixture.UserId }),
                CancellationToken.None);

        locked.IsSuccess.Should().BeTrue();
        unlocked.IsSuccess.Should().BeTrue();
        fixture.Users.Verify(
            service => service.SetLockedAsync(
                UserFixture.UserId,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Users.Verify(
            service => service.SetLockedAsync(
                UserFixture.UserId,
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Revoke sessions should return no content or the failure")]
    public async Task RevokeSessions_ShouldReturnNoContentOrFailure()
    {
        var fixture = new UserFixture();
        fixture.Users
            .Setup(service => service.RevokeSessionsAsync(
                UserFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        fixture.Users
            .Setup(service => service.RevokeSessionsAsync(
                Guid.Empty,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(UserFixture.Failure));

        Result success = await new RevokeUserSessions.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new RevokeUserSessions.UseCase(
                    new RevokeUserSessions.Request { Id = UserFixture.UserId }),
                CancellationToken.None);
        Result failure = await new RevokeUserSessions.UseCaseHandler(fixture.Users.Object)
            .Handle(
                new RevokeUserSessions.UseCase(new RevokeUserSessions.Request { Id = Guid.Empty }),
                CancellationToken.None);

        success.IsSuccess.Should().BeTrue();
        success.StatusCode.Should().Be(204);
        failure.IsFailure.Should().BeTrue();
    }

    private static CreateUser.UseCaseHandler Handler(UserFixture fixture) =>
        new(fixture.Users.Object);

    private static UpdateUser.UseCaseHandler UpdateHandler(UserFixture fixture) =>
        new(fixture.Users.Object);

    private static CreateUser.Request CreateRequest() => new()
    {
        UserName = "admin",
        Email = "admin@example.com",
        Password = Password,
        FirstName = "Admin",
    };
}
