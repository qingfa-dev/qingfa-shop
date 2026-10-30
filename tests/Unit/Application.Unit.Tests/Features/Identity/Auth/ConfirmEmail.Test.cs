using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the confirm-email slice.</summary>
public sealed class ConfirmEmailTest
{
    private static ConfirmEmail.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Confirm email should return 204 on success")]
    public async Task Handle_WhenServiceSucceeds_ShouldReturnNoContent()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.ConfirmEmailAsync(
                It.IsAny<ConfirmEmailRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new ConfirmEmail.UseCase(new ConfirmEmail.Request(
                AuthFixture.UserId.ToString(),
                "confirmation-token")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Confirm email should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.ConfirmEmailAsync(
                It.IsAny<ConfirmEmailRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthFixture.Failure));

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new ConfirmEmail.UseCase(new ConfirmEmail.Request(
                AuthFixture.UserId.ToString(),
                "confirmation-token")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Theory(DisplayName = "Confirm email validator should reject invalid identifiers or tokens")]
    [InlineData("not-a-guid", "token")]
    [InlineData("11111111-1111-1111-1111-111111111111", "")]
    public void Validate_WithInvalidRequest_ShouldFail(string userId, string token)
    {
        // Act
        var result = new ConfirmEmail.UseCaseValidator().Validate(
            new ConfirmEmail.UseCase(new ConfirmEmail.Request(userId, token)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
