using Application.Common.Security.Authentication.Identity;
using Application.Features.Identity.Auth.Commands;

namespace Application.Unit.Tests.Features.Identity.Auth;

/// <summary>Unit tests for the update-two-factor slice.</summary>
public sealed class UpdateTwoFactorTest
{
    private static UpdateTwoFactor.UseCaseHandler CreateHandler(AuthFixture fixture) =>
        new(fixture.Identity.Object, fixture.UserContext);

    [Fact(DisplayName = "Update two-factor should map the updated state")]
    public async Task Handle_ShouldMapTwoFactorState()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.UpdateTwoFactorAsync(
                AuthFixture.UserId,
                It.IsAny<TwoFactorRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorInfo>.Ok(AuthFixture.TwoFactor));

        // Act
        Result<UpdateTwoFactor.Response> result = await CreateHandler(fixture).Handle(
            new UpdateTwoFactor.UseCase(new UpdateTwoFactor.Request(Enable: false)),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsTwoFactorEnabled.Should().BeTrue();
        result.Value.RecoveryCodes.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Update two-factor without a user id should return 401")]
    public async Task Handle_WithoutUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var fixture = new AuthFixture(userId: string.Empty);

        // Act
        Result<UpdateTwoFactor.Response> result = await CreateHandler(fixture).Handle(
            new UpdateTwoFactor.UseCase(new UpdateTwoFactor.Request(Enable: false)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        fixture.Identity.Verify(
            service => service.UpdateTwoFactorAsync(
                It.IsAny<Guid>(),
                It.IsAny<TwoFactorRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Update two-factor should preserve a service failure")]
    public async Task Handle_WhenServiceFails_ShouldPreserveFailure()
    {
        // Arrange
        var fixture = new AuthFixture();
        fixture.Identity
            .Setup(service => service.UpdateTwoFactorAsync(
                It.IsAny<Guid>(),
                It.IsAny<TwoFactorRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TwoFactorInfo>.Fail(AuthFixture.Failure));

        // Act
        Result<UpdateTwoFactor.Response> result = await CreateHandler(fixture).Handle(
            new UpdateTwoFactor.UseCase(new UpdateTwoFactor.Request(Enable: false)),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Identity.Test.Failed");
    }

    [Fact(DisplayName = "Update two-factor validator should require a code when enabling")]
    public void Validate_WithoutCodeWhenEnabling_ShouldFail()
    {
        // Act
        var result = new UpdateTwoFactor.UseCaseValidator().Validate(
            new UpdateTwoFactor.UseCase(new UpdateTwoFactor.Request(Enable: true)));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "Update two-factor validator should accept a disable request without a code")]
    public void Validate_WithoutCodeWhenDisabling_ShouldPass()
    {
        // Act
        var result = new UpdateTwoFactor.UseCaseValidator().Validate(
            new UpdateTwoFactor.UseCase(new UpdateTwoFactor.Request(Enable: false)));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
