using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the register-user slice.</summary>
public sealed class RegisterUserTest
{
    private static RegisterUser.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Register should map the created user")]
    public async Task Handle_ShouldMapCreatedUser()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.RegisterAsync(
                It.IsAny<RegisterRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Ok(AuthFixture.User));

        // Act
        Result<RegisterUser.Response> result = await CreateHandler(fixture).Handle(
            new RegisterUser.UseCase(new RegisterUser.Request(
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
        Result<RegisterUser.Response> result = await CreateHandler(fixture).Handle(
            new RegisterUser.UseCase(new RegisterUser.Request(
                "admin",
                "admin@example.com",
                "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Theory(DisplayName = "Register validator should reject invalid fields")]
    [InlineData("", "admin@example.com", "Password@123")]
    [InlineData("ab", "admin@example.com", "Password@123")]
    [InlineData("admin", "not-an-email", "Password@123")]
    [InlineData("admin", "admin@example.com", "short")]
    public void Validate_WithInvalidRequest_ShouldFail(
        string userName,
        string email,
        string password)
    {
        // Act
        var result = new RegisterUser.UseCaseValidator().Validate(
            new RegisterUser.UseCase(new RegisterUser.Request(userName, email, password)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
