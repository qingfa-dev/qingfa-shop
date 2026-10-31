using Application.BuildingBlocks.Security.Identity.Auth.Commands;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth;

/// <summary>Unit tests for the forgot-password slice.</summary>
public sealed class ForgotPasswordTest
{
    private static ForgotPassword.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Forgot password should return 204 on success")]
    public async Task Handle_WhenServiceSucceeds_ShouldReturnNoContent()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.ForgotPasswordAsync(
                It.IsAny<ForgotPasswordRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new ForgotPassword.UseCase(new ForgotPasswordRequest("admin@example.com")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Forgot password should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.ForgotPasswordAsync(
                It.IsAny<ForgotPasswordRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthFixture.Failure));

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new ForgotPassword.UseCase(new ForgotPasswordRequest("admin@example.com")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }
}
