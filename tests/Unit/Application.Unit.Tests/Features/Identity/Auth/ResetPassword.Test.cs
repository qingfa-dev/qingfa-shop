using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the reset-password slice.</summary>
public sealed class ResetPasswordTest
{
    private static ResetPassword.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Reset password should return 204 on success")]
    public async Task Handle_WhenServiceSucceeds_ShouldReturnNoContent()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.ResetPasswordAsync(
                It.IsAny<ResetPasswordRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new ResetPassword.UseCase(new ResetPassword.Request(
                "admin@example.com",
                "reset-token",
                "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Reset password should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.ResetPasswordAsync(
                It.IsAny<ResetPasswordRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthFixture.Failure));

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new ResetPassword.UseCase(new ResetPassword.Request(
                "admin@example.com",
                "reset-token",
                "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Theory(DisplayName = "Reset password validator should reject invalid fields")]
    [InlineData("not-an-email", "reset-token", "Password@123")]
    [InlineData("admin@example.com", "", "Password@123")]
    [InlineData("admin@example.com", "reset-token", "short")]
    public void Validate_WithInvalidRequest_ShouldFail(
        string email,
        string token,
        string newPassword)
    {
        // Act
        var result = new ResetPassword.UseCaseValidator().Validate(
            new ResetPassword.UseCase(new ResetPassword.Request(email, token, newPassword)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
