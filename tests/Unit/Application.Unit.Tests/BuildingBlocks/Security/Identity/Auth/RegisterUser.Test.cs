using Application.BuildingBlocks.Security.Identity.Auth.Commands;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth;

/// <summary>Unit tests for the register-user slice.</summary>
public sealed class RegisterUserTest
{
    private static RegisterUser.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Register should return the created user")]
    public async Task Handle_ShouldReturnCreatedUser()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Ok(AuthFixture.User));

        // Act
        Result<IdentityUserModel> result = await CreateHandler(fixture).Handle(
            new RegisterUser.UseCase(new RegisterRequest(
                "admin",
                "admin@example.com",
                "Password@123",
                "Admin")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(AuthFixture.UserId);
        result.Value.UserName.Should().Be("admin");
        result.Value.EmailConfirmed.Should().BeTrue();
    }

    [Fact(DisplayName = "Register should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Fail(AuthFixture.Failure));

        // Act
        Result<IdentityUserModel> result = await CreateHandler(fixture).Handle(
            new RegisterUser.UseCase(new RegisterRequest(
                "admin",
                "admin@example.com",
                "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }
}
