using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the update-current-user slice.</summary>
public sealed class UpdateCurrentUserTest
{
    private static UpdateCurrentUser.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object, fixture.UserContext);

    [Fact(DisplayName = "Update current user should map the updated profile")]
    public async Task Handle_ShouldMapUpdatedUser()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.UpdateInfoAsync(
                AuthFixture.UserId,
                It.IsAny<UpdateInfoRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Ok(AuthFixture.User));

        // Act
        Result<UpdateCurrentUser.Response> result = await CreateHandler(fixture).Handle(
            new UpdateCurrentUser.UseCase(new UpdateCurrentUser.Request(FirstName: "Admin")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(AuthFixture.UserId);
        result.Value.FirstName.Should().Be("Admin");
    }

    [Fact(DisplayName = "Update current user without a user id should return 401")]
    public async Task Handle_WithoutUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var fixture = new AuthFixture(userId: "not-a-guid");

        // Act
        Result<UpdateCurrentUser.Response> result = await CreateHandler(fixture).Handle(
            new UpdateCurrentUser.UseCase(new UpdateCurrentUser.Request(FirstName: "Admin")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Authentication.UserIdMissing");
        fixture.Identity.Verify(
            service => service.UpdateInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<UpdateInfoRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Update current user should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.UpdateInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<UpdateInfoRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Fail(AuthFixture.Failure));

        // Act
        Result<UpdateCurrentUser.Response> result = await CreateHandler(fixture).Handle(
            new UpdateCurrentUser.UseCase(new UpdateCurrentUser.Request(FirstName: "Admin")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Fact(DisplayName = "Update current user validator should reject a new password without the old one")]
    public void Validate_WithNewPasswordWithoutOld_ShouldFail()
    {
        // Act
        var result = new UpdateCurrentUser.UseCaseValidator().Validate(
            new UpdateCurrentUser.UseCase(
                new UpdateCurrentUser.Request(NewPassword: "Password@123")));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Update current user validator should reject an invalid optional email")]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        // Act
        var result = new UpdateCurrentUser.UseCaseValidator().Validate(
            new UpdateCurrentUser.UseCase(
                new UpdateCurrentUser.Request(Email: "not-an-email")));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
