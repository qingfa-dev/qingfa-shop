using Application.Features.Identity.Auth.Commands;
using Application.Features.Identity.Auth.Models;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the update-current-user slice.</summary>
public sealed class UpdateCurrentUserTest
{
    private static UpdateCurrentUser.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object, fixture.UserContext);

    [Fact(DisplayName = "Update current user should return the updated profile")]
    public async Task Handle_ShouldReturnUpdatedUser()
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
        Result<IdentityUserModel> result = await CreateHandler(fixture).Handle(
            new UpdateCurrentUser.UseCase(new UpdateInfoRequest(FirstName: "Admin")),
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
        Result<IdentityUserModel> result = await CreateHandler(fixture).Handle(
            new UpdateCurrentUser.UseCase(new UpdateInfoRequest(FirstName: "Admin")),
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
        Result<IdentityUserModel> result = await CreateHandler(fixture).Handle(
            new UpdateCurrentUser.UseCase(new UpdateInfoRequest(FirstName: "Admin")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }
}
