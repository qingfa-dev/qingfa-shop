using Application.BuildingBlocks.Security.Identity.Auth;
using Application.BuildingBlocks.Security.Identity.External.Mappings;
using Application.BuildingBlocks.Security.Identity.External.Models;
using Application.BuildingBlocks.Security.Identity.External.Options;

namespace Application.Unit.Tests.BuildingBlocks.Security.Identity.External.Mappings;

/// <summary>Unit tests for the external sign-in mappings.</summary>
public sealed class ExternalMapperTest
{
    [Theory(DisplayName = "A configured provider should resolve regardless of route casing")]
    [InlineData("google")]
    [InlineData("Google")]
    [InlineData("GOOGLE")]
    public void ResolveProvider_WithConfiguredProvider_ShouldReturnProvider(string provider)
    {
        Result<ExternalProviderModel> result = ExternalFixture.Providers
            .ResolveProvider(provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(IdentityConstant.Providers.Google);
    }

    [Theory(DisplayName = "The openid route alias should resolve to OpenID Connect")]
    [InlineData("openid")]
    [InlineData("openidconnect")]
    [InlineData("OpenIdConnect")]
    public void ResolveProvider_WithOpenIdConnectAlias_ShouldReturnProvider(string provider)
    {
        Result<ExternalProviderModel> result = ExternalFixture.Providers
            .ResolveProvider(provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(IdentityConstant.Providers.OpenIdConnect);
    }

    [Theory(DisplayName = "An unknown or missing provider should produce a 404 failure")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("github")]
    public void ResolveProvider_WithUnknownProvider_ShouldReturn404(string? provider)
    {
        Result<ExternalProviderModel> result = ExternalFixture.Providers
            .ResolveProvider(provider);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.ProviderUnsupported");
    }

    [Fact(DisplayName = "Provider resolution should reject a missing provider list")]
    public void ResolveProvider_WithoutProviders_ShouldThrow()
    {
        Action act = () => ((IReadOnlyCollection<ExternalProviderModel>)null!).ResolveProvider("google");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "The callback path should use the lowercase provider segment")]
    [InlineData("Google", "google", "/api/auth/external/google/callback")]
    [InlineData("Microsoft", "microsoft", "/api/auth/external/microsoft/callback")]
    [InlineData("OpenIdConnect", "openid", "/api/auth/external/openid/callback")]
    public void CallbackPath_ShouldUseTheProviderSegment(
        string scheme,
        string expectedSegment,
        string expectedPath)
    {
        string segment = ExternalMapper.ProviderSegment(scheme);

        segment.Should().Be(expectedSegment);
        ExternalMapper.CallbackPath(segment).Should().Be(expectedPath);
    }

    [Fact(DisplayName = "Provider segments and callback paths should reject missing inputs")]
    public void ProviderSegment_WithMissingInputs_ShouldThrow()
    {
        Action nullSegment = () => ExternalMapper.ProviderSegment(null!);
        Action blankSegment = () => ExternalMapper.ProviderSegment(" ");
        Action blankCallback = () => ExternalMapper.CallbackPath(string.Empty);

        nullSegment.Should().Throw<ArgumentException>();
        blankSegment.Should().Throw<ArgumentException>();
        blankCallback.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "The challenge should use the handler's configured callback path")]
    [InlineData("Google", "/api/auth/external/google/callback")]
    [InlineData("Microsoft", "/api/auth/external/microsoft/callback")]
    public void CallbackRedirectUri_ForOAuthProvider_ShouldUseTheRoute(
        string scheme,
        string expected)
    {
        var provider = new ExternalProviderModel(scheme, scheme);

        ExternalMapper.CallbackRedirectUri(provider, new ExternalAuthOption())
            .Should().Be(expected);
    }

    [Fact(DisplayName = "The challenge should honor a configured OpenID Connect callback path")]
    public void CallbackRedirectUri_ForOpenIdConnect_ShouldUseConfiguredPath()
    {
        var provider = new ExternalProviderModel(
            IdentityConstant.Providers.OpenIdConnect,
            "OpenID Connect");
        var option = new ExternalAuthOption();
        option.OpenIdConnect.CallbackPath = "/custom/signin-oidc";

        ExternalMapper.CallbackRedirectUri(provider, option)
            .Should().Be("/custom/signin-oidc");
    }

    [Fact(DisplayName = "OpenID Connect should fall back to the API callback route")]
    public void CallbackRedirectUri_ForOpenIdConnectWithoutOverride_ShouldUseDefault()
    {
        var provider = new ExternalProviderModel(
            IdentityConstant.Providers.OpenIdConnect,
            "OpenID Connect");
        var option = new ExternalAuthOption();
        option.OpenIdConnect.CallbackPath = string.Empty;

        ExternalMapper.CallbackRedirectUri(provider, option)
            .Should().Be("/api/auth/external/openid/callback");
    }

    [Fact(DisplayName = "The challenge mapping should reject missing inputs")]
    public void CallbackRedirectUri_WithMissingInputs_ShouldThrow()
    {
        var provider = new ExternalProviderModel(
            IdentityConstant.Providers.Google,
            "Google");

        Action nullProvider = () => ExternalMapper.CallbackRedirectUri(null!, new ExternalAuthOption());
        Action nullOption = () => ExternalMapper.CallbackRedirectUri(provider, null!);

        nullProvider.Should().Throw<ArgumentNullException>();
        nullOption.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "A provider should map to the challenge model")]
    public void ToChallenge_ShouldReturnProviderAndRedirectUri()
    {
        var provider = new ExternalProviderModel(
            IdentityConstant.Providers.Google,
            "Google");

        ExternalChallengeModel challenge = provider.ToChallenge(new ExternalAuthOption());

        challenge.Provider.Should().Be(IdentityConstant.Providers.Google);
        challenge.RedirectUri.Should().Be("/api/auth/external/google/callback");
    }

    [Fact(DisplayName = "The challenge mapping should reject a missing provider")]
    public void ToChallenge_WithoutProvider_ShouldThrow()
    {
        Action act = () => ((ExternalProviderModel)null!).ToChallenge(new ExternalAuthOption());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "A callback request should map to the external user information")]
    public void ToUserInfo_ShouldMapNormalizedIdentity()
    {
        var request = new ExternalCallbackRequestModel
        {
            Provider = IdentityConstant.Providers.Google,
            ProviderKey = "google-user-1",
            DisplayName = "External User",
            Email = "external@example.com",
        };

        ExternalUserInfo user = request.ToUserInfo();

        user.Should().BeEquivalentTo(new ExternalUserInfo(
            IdentityConstant.Providers.Google,
            "google-user-1",
            "External User",
            "external@example.com"));
    }

    [Fact(DisplayName = "The callback mapping should reject a missing request")]
    public void ToUserInfo_WithoutRequest_ShouldThrow()
    {
        Action act = () => ((ExternalCallbackRequestModel)null!).ToUserInfo();

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "The one-time code should be appended to the client redirect URL")]
    [InlineData("/", "/?code=abc")]
    [InlineData("", "/?code=abc")]
    [InlineData("https://client.example/callback", "https://client.example/callback?code=abc")]
    [InlineData("https://client.example/callback?state=1", "https://client.example/callback?state=1&code=abc")]
    [InlineData("https://client.example/callback&", "https://client.example/callback&code=abc")]
    [InlineData("https://client.example/callback?", "https://client.example/callback?code=abc")]
    public void BuildRedirectUrl_ShouldAppendTheCode(string redirectUrl, string expected)
    {
        ExternalMapper.BuildRedirectUrl(redirectUrl, "abc").Should().Be(expected);
    }

    [Fact(DisplayName = "The redirect builder should reject a missing code")]
    public void BuildRedirectUrl_WithoutCode_ShouldThrow()
    {
        Action act = () => ExternalMapper.BuildRedirectUrl("/", " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "The callback model should wrap the client redirect URL")]
    public void ToCallback_ShouldReturnRedirectModel()
    {
        ExternalCallbackModel callback = ExternalMapper.ToCallback(
            "https://client.example/callback",
            "abc");

        callback.RedirectUrl.Should().Be("https://client.example/callback?code=abc");
    }
}
