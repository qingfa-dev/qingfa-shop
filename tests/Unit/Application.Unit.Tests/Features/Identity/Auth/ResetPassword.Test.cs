using Application.Features.Identity.Auth.Commands;
using Application.Features.Identity.Auth.Models;

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
            new ResetPassword.UseCase(new ResetPasswordRequest(
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
            new ResetPassword.UseCase(new ResetPasswordRequest(
                "admin@example.com",
                "reset-token",
                "Password@123")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }
}
