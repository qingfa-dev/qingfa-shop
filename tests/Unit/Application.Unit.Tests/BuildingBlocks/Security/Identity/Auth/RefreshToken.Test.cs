using Application.BuildingBlocks.Security.Identity.Auth.Commands;
using Application.BuildingBlocks.Security.Identity.Auth.Models;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.Auth;

/// <summary>Unit tests for the refresh-token slice.</summary>
public sealed class RefreshTokenTest
{
    private static RefreshToken.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Refresh should return the rotated token pair")]
    public async Task Handle_ShouldReturnRotatedTokens()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.RefreshAsync(
                It.IsAny<RefreshRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Ok(AuthFixture.Tokens()));

        // Act
        Result<AuthTokenResponse> result = await CreateHandler(fixture).Handle(
            new RefreshToken.UseCase(new RefreshRequest("refresh-token")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact(DisplayName = "Refresh should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.RefreshAsync(
                It.IsAny<RefreshRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Fail(AuthFixture.Failure));

        // Act
        Result<AuthTokenResponse> result = await CreateHandler(fixture).Handle(
            new RefreshToken.UseCase(new RefreshRequest("refresh-token")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }
}
