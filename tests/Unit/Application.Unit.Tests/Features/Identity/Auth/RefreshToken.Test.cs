using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the refresh-token slice.</summary>
public sealed class RefreshTokenTest
{
    private static RefreshToken.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object);

    [Fact(DisplayName = "Refresh should map the rotated token pair")]
    public async Task Handle_ShouldMapRotatedTokens()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.RefreshAsync(
                It.IsAny<RefreshRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Ok(AuthFixture.Tokens()));

        // Act
        Result<RefreshToken.Response> result = await CreateHandler(fixture).Handle(
            new RefreshToken.UseCase(new RefreshToken.Request("refresh-token")),
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
            .ReturnsAsync(Result<AuthResponse>.Fail(AuthFixture.Failure));

        // Act
        Result<RefreshToken.Response> result = await CreateHandler(fixture).Handle(
            new RefreshToken.UseCase(new RefreshToken.Request("refresh-token")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Fact(DisplayName = "Refresh validator should require the refresh token")]
    public void Validate_WithEmptyToken_ShouldFail()
    {
        // Act
        var result = new RefreshToken.UseCaseValidator().Validate(
            new RefreshToken.UseCase(new RefreshToken.Request(string.Empty)));

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
