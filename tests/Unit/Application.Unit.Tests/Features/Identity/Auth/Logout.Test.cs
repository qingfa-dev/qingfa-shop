using Application.Features.Identity.Auth.Commands;
using Application.Features.Identity.Auth.Models;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the logout slice.</summary>
public sealed class LogoutTest
{
    private static Logout.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Logout should return 204 on success")]
    public async Task Handle_WhenServiceSucceeds_ShouldReturnNoContent()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.LogoutAsync(
                It.IsAny<LogoutRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new Logout.UseCase(new LogoutRequest("refresh-token", "jti", AuthFixture.Now)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(204);
    }

    [Fact(DisplayName = "Logout should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.LogoutAsync(
                It.IsAny<LogoutRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(AuthFixture.Failure));

        // Act
        Result result = await CreateHandler(fixture).Handle(
            new Logout.UseCase(new LogoutRequest("refresh-token")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
    }

    [Fact(DisplayName = "Logout handler should reject a missing command or request")]
    public async Task Handle_WithMissingInputs_ShouldThrow()
    {
        // Arrange
        var fixture = new AuthFixture();
        Logout.UseCaseHandler handler = CreateHandler(fixture);

        Func<Task> missingCommand = () => handler.Handle(null!, CancellationToken.None);
        Func<Task> missingRequest = () => handler.Handle(
            new Logout.UseCase(null!),
            CancellationToken.None);

        // Act & Assert
        await missingCommand.Should().ThrowAsync<ArgumentNullException>();
        await missingRequest.Should().ThrowAsync<ArgumentNullException>();
    }
}
