using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

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
            new ForgotPassword.UseCase(new ForgotPassword.Request("admin@example.com")),
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
            new ForgotPassword.UseCase(new ForgotPassword.Request("admin@example.com")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Theory(DisplayName = "Forgot password validator should reject an invalid email")]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        // Act
        var result = new ForgotPassword.UseCaseValidator().Validate(
            new ForgotPassword.UseCase(new ForgotPassword.Request(email)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
