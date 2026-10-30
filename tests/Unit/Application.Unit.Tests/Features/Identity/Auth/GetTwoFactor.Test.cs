using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Queries;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the get-two-factor query.</summary>
public sealed class GetTwoFactorTest
{
    private static GetTwoFactor.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object, fixture.UserContext);

    [Fact(DisplayName = "Get two-factor should map the current state")]
    public async Task Handle_ShouldMapTwoFactorState()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.GetTwoFactorInfoAsync(
                AuthFixture.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorInfo>.Ok(AuthFixture.TwoFactor));

        // Act
        Result<GetTwoFactor.Response> result = await CreateHandler(fixture).Handle(
            new GetTwoFactor.UseCase(new GetTwoFactor.Request()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsTwoFactorEnabled.Should().BeTrue();
        result.Value.SharedKey.Should().Be("shared-key");
        result.Value.RecoveryCodes.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Get two-factor without a user id should return 401")]
    public async Task Handle_WithoutUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var fixture = new AuthFixture(userId: string.Empty);

        // Act
        Result<GetTwoFactor.Response> result = await CreateHandler(fixture).Handle(
            new GetTwoFactor.UseCase(new GetTwoFactor.Request()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        fixture.Identity.Verify(
            service => service.GetTwoFactorInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Get two-factor should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.GetTwoFactorInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorInfo>.Fail(AuthFixture.Failure));

        // Act
        Result<GetTwoFactor.Response> result = await CreateHandler(fixture).Handle(
            new GetTwoFactor.UseCase(new GetTwoFactor.Request()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Fact(DisplayName = "Get two-factor validator should accept the empty request")]
    public void Validate_WithEmptyRequest_ShouldPass()
    {
        // Act
        var result = new GetTwoFactor.UseCaseValidator().Validate(
            new GetTwoFactor.UseCase(new GetTwoFactor.Request()));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
