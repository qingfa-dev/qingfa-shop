using System.Globalization;
using System.Security.Claims;

using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

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
            new Logout.UseCase(new Logout.Request("refresh-token", "jti", AuthFixture.Now)),
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
            new Logout.UseCase(new Logout.Request("refresh-token")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
    }

    [Fact(DisplayName = "Logout should read the jti and exp claims into the request")]
    public void FromPrincipal_ShouldForwardPrincipalClaims()
    {
        // Arrange
        var principal = Principal(
            new Claim("jti", "access-jti"),
            new Claim("exp", "1893456000"));

        // Act
        Logout.Request request = Logout.FromPrincipal(
            new Logout.Request("refresh-token"),
            principal);

        // Assert
        request.RefreshToken.Should().Be("refresh-token");
        request.AccessTokenJti.Should().Be("access-jti");
        request.AccessTokenExpiresAtUtc.Should().Be(
            DateTimeOffset.FromUnixTimeSeconds(1893456000));
    }

    [Fact(DisplayName = "Logout without a body should still forward the principal claims")]
    public void FromPrincipal_WithoutBody_ShouldKeepPrincipalClaims()
    {
        // Arrange
        var principal = Principal(
            new Claim("jti", "access-jti"),
            new Claim("exp", "1893456000"));

        // Act
        Logout.Request request = Logout.FromPrincipal(null, principal);

        // Assert
        request.RefreshToken.Should().BeNull();
        request.AccessTokenJti.Should().Be("access-jti");
        request.AccessTokenExpiresAtUtc.Should().NotBeNull();
    }

    [Theory(DisplayName = "The exp claim should map boundary values to their UTC instant")]
    [InlineData("0")]
    [InlineData("1893456000")]
    [InlineData("253402300799")]
    public void ReadAccessTokenExpiry_WithValidClaim_ShouldReturnInstant(string value)
    {
        // Arrange
        var principal = Principal(new Claim("exp", value));

        // Act
        DateTimeOffset? expiresAt = Logout.ReadAccessTokenExpiry(principal);

        // Assert
        expiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(long.Parse(
            value,
            CultureInfo.InvariantCulture)));
    }

    [Theory(DisplayName = "An absent, malformed, or out-of-range exp claim should be ignored")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    [InlineData("253402300800")]
    [InlineData("9223372036854775807")]
    public void ReadAccessTokenExpiry_WithoutValidClaim_ShouldReturnNull(string? value)
    {
        // Arrange
        var principal = Principal(
            value is null ? [] : [new Claim("exp", value)]);

        // Act
        DateTimeOffset? expiresAt = Logout.ReadAccessTokenExpiry(principal);

        // Assert
        expiresAt.Should().BeNull();
    }

    [Fact(DisplayName = "Logout helpers should reject missing inputs")]
    public void Helpers_WithMissingInputs_ShouldThrow()
    {
        // Arrange
        Action nullPrincipal = () => Logout.FromPrincipal(null, null!);
        Action nullExpiryPrincipal = () => Logout.ReadAccessTokenExpiry(null!);

        // Act & Assert
        nullPrincipal.Should().Throw<ArgumentNullException>();
        nullExpiryPrincipal.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Logout validator should accept a request without a body")]
    public void Validate_WithEmptyRequest_ShouldPass()
    {
        // Act
        var result = new Logout.UseCaseValidator().Validate(
            new Logout.UseCase(new Logout.Request()));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));
}
