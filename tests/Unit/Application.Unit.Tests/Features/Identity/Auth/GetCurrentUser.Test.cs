using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Queries;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the get-current-user query.</summary>
public sealed class GetCurrentUserTest
{
    private static GetCurrentUser.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object, fixture.UserContext);

    [Fact(DisplayName = "Get current user should map the profile")]
    public async Task Handle_ShouldMapCurrentUser()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.GetInfoAsync(
                AuthFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Ok(AuthFixture.User));

        // Act
        Result<GetCurrentUser.Response> result = await CreateHandler(fixture).Handle(
            new GetCurrentUser.UseCase(new GetCurrentUser.Request()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(AuthFixture.UserId);
        result.Value.UserName.Should().Be("admin");
    }

    [Fact(DisplayName = "Get current user without a user id should return 401")]
    public async Task Handle_WithoutUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var fixture = new AuthFixture(userId: "not-a-guid");

        // Act
        Result<GetCurrentUser.Response> result = await CreateHandler(fixture).Handle(
            new GetCurrentUser.UseCase(new GetCurrentUser.Request()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Authentication.UserIdMissing");
        fixture.Identity.Verify(
            service => service.GetInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Get current user should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.GetInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IdentityUserModel>.Fail(AuthFixture.Failure));

        // Act
        Result<GetCurrentUser.Response> result = await CreateHandler(fixture).Handle(
            new GetCurrentUser.UseCase(new GetCurrentUser.Request()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Fact(DisplayName = "Get current user validator should accept the empty request")]
    public void Validate_WithEmptyRequest_ShouldPass()
    {
        // Act
        var result = new GetCurrentUser.UseCaseValidator().Validate(
            new GetCurrentUser.UseCase(new GetCurrentUser.Request()));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
