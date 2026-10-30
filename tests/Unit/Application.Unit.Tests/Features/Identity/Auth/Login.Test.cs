using Application.Features.Identity.Auth.Commands;
using Application.Features.Identity.Auth.Models;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the login slice.</summary>
public sealed class LoginTest
{
    private static Login.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Login should return the issued token pair")]
    public async Task Handle_ShouldReturnIssuedTokens()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Ok(AuthFixture.Tokens()));

        // Act
        Result<AuthTokenResponse> result = await CreateHandler(fixture).Handle(
            new Login.UseCase(new LoginRequest(
                "admin",
                "Password@123",
                "123456",
                "recovery-code")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresAtUtc.Should().Be(AuthFixture.Now.AddMinutes(15));
        result.Value.User.Should().Be(AuthFixture.User);
    }

    [Fact(DisplayName = "Login should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Fail(AuthFixture.Failure));

        // Act
        Result<AuthTokenResponse> result = await CreateHandler(fixture).Handle(
            new Login.UseCase(new LoginRequest("admin", "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }
}
