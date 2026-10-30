using Application.Features.Identity.Auth;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.External.Commands;
using Application.Features.Identity.External.Models;

namespace Application.Unit.Tests.Features.Identity.External.Commands;

/// <summary>Unit tests for the external sign-in command handlers.</summary>
public sealed class ExternalCommandsTest
{
    [Fact(DisplayName = "Challenge should resolve the provider and its callback path")]
    public async Task Challenge_ShouldReturnProviderAndCallbackPath()
    {
        var fixture = new ExternalFixture();
        fixture.External.Setup(service => service.GetProviders())
            .Returns(ExternalFixture.Providers);

        Result<ExternalChallengeModel> result = await new ExternalChallenge.UseCaseHandler(
                fixture.External.Object,
                fixture.Option)
            .Handle(
                new ExternalChallenge.UseCase(new ExternalChallenge.Request
                {
                    Provider = "google",
                }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be(IdentityConstant.Providers.Google);
        result.Value.RedirectUri.Should().Be("/api/auth/external/google/callback");
    }

    [Fact(DisplayName = "Challenge should honor the configured OpenID Connect callback path")]
    public async Task Challenge_WithOpenIdConnectAlias_ShouldUseConfiguredCallbackPath()
    {
        var fixture = new ExternalFixture();
        fixture.Option.OpenIdConnect.CallbackPath = "/custom/signin-oidc";
        fixture.External.Setup(service => service.GetProviders())
            .Returns(ExternalFixture.Providers);

        Result<ExternalChallengeModel> result = await new ExternalChallenge.UseCaseHandler(
                fixture.External.Object,
                fixture.Option)
            .Handle(
                new ExternalChallenge.UseCase(new ExternalChallenge.Request
                {
                    Provider = "openid",
                }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be(IdentityConstant.Providers.OpenIdConnect);
        result.Value.RedirectUri.Should().Be("/custom/signin-oidc");
    }

    [Theory(DisplayName = "Challenge should reject an unconfigured provider")]
    [InlineData("")]
    [InlineData("github")]
    public async Task Challenge_WithUnknownProvider_ShouldReturn404(string provider)
    {
        var fixture = new ExternalFixture();
        fixture.External.Setup(service => service.GetProviders())
            .Returns(ExternalFixture.Providers);

        Result<ExternalChallengeModel> result = await new ExternalChallenge.UseCaseHandler(
                fixture.External.Object,
                fixture.Option)
            .Handle(
                new ExternalChallenge.UseCase(new ExternalChallenge.Request
                {
                    Provider = provider,
                }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.ProviderUnsupported");
    }

    [Fact(DisplayName = "Challenge should reject a missing request")]
    public async Task Challenge_WithoutRequest_ShouldThrow()
    {
        var fixture = new ExternalFixture();
        var handler = new ExternalChallenge.UseCaseHandler(
            fixture.External.Object,
            fixture.Option);

        Func<Task> act = () => handler.Handle(
            new ExternalChallenge.UseCase(null!),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Callback should log in, store the code, and build the client redirect")]
    public async Task Callback_ShouldStoreCodeAndReturnRedirect()
    {
        var fixture = new ExternalFixture();
        fixture.External
            .Setup(service => service.LoginAsync(
                It.Is<ExternalUserInfo>(user =>
                    user.Provider == IdentityConstant.Providers.Google &&
                    user.ProviderKey == "google-user-1" &&
                    user.DisplayName == "External User" &&
                    user.Email == "external@example.com"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Ok(ExternalFixture.Token));
        fixture.Codes
            .Setup(store => store.StoreAsync(
                It.IsAny<AuthTokenResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("one-time-code"));

        Result<ExternalCallbackModel> result = await new ExternalCallback.UseCaseHandler(
                fixture.External.Object,
                fixture.Codes.Object,
                fixture.Option)
            .Handle(
                new ExternalCallback.UseCase(new ExternalCallback.Request
                {
                    Provider = IdentityConstant.Providers.Google,
                    ProviderKey = "google-user-1",
                    DisplayName = "External User",
                    Email = "external@example.com",
                }),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RedirectUrl.Should().Be(
            "https://client.example/callback?code=one-time-code");
    }

    [Fact(DisplayName = "Callback should preserve an external login failure")]
    public async Task Callback_WhenLoginFails_ShouldPreserveFailure()
    {
        var fixture = new ExternalFixture();
        fixture.External
            .Setup(service => service.LoginAsync(
                It.IsAny<ExternalUserInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Fail(ExternalFixture.Failure));

        Result<ExternalCallbackModel> result = await new ExternalCallback.UseCaseHandler(
                fixture.External.Object,
                fixture.Codes.Object,
                fixture.Option)
            .Handle(
                new ExternalCallback.UseCase(new ExternalCallback.Request
                {
                    Provider = IdentityConstant.Providers.Google,
                    ProviderKey = "google-user-1",
                }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().ContainSingle(
            error => error.Code == "External.Test.NotFound");
        fixture.Codes.Verify(
            store => store.StoreAsync(
                It.IsAny<AuthTokenResponse>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Callback should preserve a code storage failure")]
    public async Task Callback_WhenCodeStoreFails_ShouldPreserveFailure()
    {
        var fixture = new ExternalFixture();
        fixture.External
            .Setup(service => service.LoginAsync(
                It.IsAny<ExternalUserInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Ok(ExternalFixture.Token));
        fixture.Codes
            .Setup(store => store.StoreAsync(
                It.IsAny<AuthTokenResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Fail(ExternalFixture.Failure));

        Result<ExternalCallbackModel> result = await new ExternalCallback.UseCaseHandler(
                fixture.External.Object,
                fixture.Codes.Object,
                fixture.Option)
            .Handle(
                new ExternalCallback.UseCase(new ExternalCallback.Request
                {
                    Provider = IdentityConstant.Providers.Google,
                    ProviderKey = "google-user-1",
                }),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Callback should reject a missing request")]
    public async Task Callback_WithoutRequest_ShouldThrow()
    {
        var fixture = new ExternalFixture();
        var handler = new ExternalCallback.UseCaseHandler(
            fixture.External.Object,
            fixture.Codes.Object,
            fixture.Option);

        Func<Task> act = () => handler.Handle(
            new ExternalCallback.UseCase(null!),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Exchange should consume the one-time code")]
    public async Task Exchange_ShouldConsumeCode()
    {
        var fixture = new ExternalFixture();
        fixture.Codes
            .Setup(store => store.ConsumeAsync("one-time-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Ok(ExternalFixture.Token));

        Result<AuthTokenResponse> result = await new ExchangeExternalCode.UseCaseHandler(
                fixture.Codes.Object)
            .Handle(
                new ExchangeExternalCode.UseCase(new ExternalExchangeRequest("one-time-code")),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        fixture.Codes.Verify(
            store => store.ConsumeAsync("one-time-code", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory(DisplayName = "Exchange should reject a missing or blank code as 401")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Exchange_WithoutCode_ShouldReturn401(string? code)
    {
        var fixture = new ExternalFixture();
        ExternalExchangeRequest? request = code is null
            ? null
            : new ExternalExchangeRequest(code);

        Result<AuthTokenResponse> result = await new ExchangeExternalCode.UseCaseHandler(
                fixture.Codes.Object)
            .Handle(
                new ExchangeExternalCode.UseCase(request),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.CodeInvalid");
        fixture.Codes.Verify(
            store => store.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Exchange should preserve a store failure")]
    public async Task Exchange_WhenStoreFails_ShouldPreserveFailure()
    {
        var fixture = new ExternalFixture();
        fixture.Codes
            .Setup(store => store.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthTokenResponse>.Fail(ExternalFixture.Failure));

        Result<AuthTokenResponse> result = await new ExchangeExternalCode.UseCaseHandler(
                fixture.Codes.Object)
            .Handle(
                new ExchangeExternalCode.UseCase(new ExternalExchangeRequest("one-time-code")),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact(DisplayName = "Exchange should reject a missing command")]
    public async Task Exchange_WithoutCommand_ShouldThrow()
    {
        var fixture = new ExternalFixture();
        var handler = new ExchangeExternalCode.UseCaseHandler(fixture.Codes.Object);

        Func<Task> act = () => handler.Handle(
            null!,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
