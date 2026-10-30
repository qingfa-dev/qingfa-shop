using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the login slice.</summary>
public sealed class LoginTest
{
    private static Login.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Login should map the issued token pair")]
    public async Task Handle_ShouldMapIssuedTokens()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Ok(AuthFixture.Tokens()));

        // Act
        Result<Login.Response> result = await CreateHandler(fixture).Handle(
            new Login.UseCase(new Login.Request(
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
            .ReturnsAsync(Result<AuthResponse>.Fail(AuthFixture.Failure));

        // Act
        Result<Login.Response> result = await CreateHandler(fixture).Handle(
            new Login.UseCase(new Login.Request("admin", "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Theory(DisplayName = "Login validator should require credentials")]
    [InlineData("", "Password@123")]
    [InlineData("admin", "")]
    public void Validate_WithMissingCredential_ShouldFail(string identifier, string password)
    {
        // Act
        var result = new Login.UseCaseValidator().Validate(
            new Login.UseCase(new Login.Request(identifier, password)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
