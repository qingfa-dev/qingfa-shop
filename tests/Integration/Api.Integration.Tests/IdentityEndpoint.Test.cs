using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

using Application.Common.Security.Authentication.Contexts;

using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Http;

using Application.Features.Identity.Auth;
using Application.Features.Identity.Auth.Commands;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.External.Models;

using Infrastructure.Integrations.Serialization;

using Web.Api.Endpoints.Identity;
using Web.Api.Endpoints.Identity.Models;

using ExternalAuthOption = Application.Common.Security.Authentication.Identity.ExternalAuthOption;

namespace Api.Integration.Tests;

/// <summary>Locks the current-user guard and principal-claim helpers used by the identity routes.</summary>
[Trait("Category", "Unit")]
public sealed class IdentityEndpointTest
{
    [Fact(DisplayName = "A valid user identifier should pass through")]
    public void RequireUserId_WithValidIdentifier_ShouldReturnUserId()
    {
        Guid userId = Guid.NewGuid();

        Result<Guid> result = AuthGuard.RequireUserId(
            new StubUserContext(userId.ToString()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(userId);
    }

    [Theory(DisplayName = "A missing or malformed user identifier should produce a 401")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void RequireUserId_WithoutValidIdentifier_ShouldReturnUnauthorized(string? userId)
    {
        Result<Guid> result = AuthGuard.RequireUserId(new StubUserContext(userId));

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Authentication.UserIdMissing");
    }

    [Fact(DisplayName = "Logout should read the jti and exp claims into the application request")]
    public void ToLogoutRequest_ShouldForwardPrincipalClaims()
    {
        var principal = Principal(
            new Claim(JwtRegisteredClaimNames.Jti, "access-jti"),
            new Claim(JwtRegisteredClaimNames.Exp, "1893456000"));

        var mapped = IdentityEndpoint.FromPrincipal(
            new LogoutBody("refresh-token"),
            principal);

        mapped.RefreshToken.Should().Be("refresh-token");
        mapped.AccessTokenJti.Should().Be("access-jti");
        mapped.AccessTokenExpiresAtUtc.Should().Be(
            DateTimeOffset.FromUnixTimeSeconds(1893456000));
    }

    [Fact(DisplayName = "Logout without a body should still forward the principal claims")]
    public void ToLogoutRequest_WithoutBody_ShouldKeepPrincipalClaims()
    {
        var principal = Principal(
            new Claim(JwtRegisteredClaimNames.Jti, "access-jti"),
            new Claim(JwtRegisteredClaimNames.Exp, "1893456000"));

        var mapped = IdentityEndpoint.FromPrincipal(null, principal);

        mapped.RefreshToken.Should().BeNull();
        mapped.AccessTokenJti.Should().Be("access-jti");
        mapped.AccessTokenExpiresAtUtc.Should().NotBeNull();
    }

    [Theory(DisplayName = "The exp claim should map boundary values to their UTC instant")]
    [InlineData("0")]
    [InlineData("1893456000")]
    [InlineData("253402300799")]
    public void ReadAccessTokenExpiry_WithValidClaim_ShouldReturnInstant(string value)
    {
        var principal = Principal(new Claim(JwtRegisteredClaimNames.Exp, value));

        DateTimeOffset? expiresAt = IdentityEndpoint.ReadAccessTokenExpiry(principal);

        expiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(long.Parse(
            value,
            System.Globalization.CultureInfo.InvariantCulture)));
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
        var principal = Principal(
            value is null ? [] : [new Claim(JwtRegisteredClaimNames.Exp, value)]);

        DateTimeOffset? expiresAt = IdentityEndpoint.ReadAccessTokenExpiry(principal);

        expiresAt.Should().BeNull();
    }

    [Theory(DisplayName = "A configured provider should resolve regardless of route casing")]
    [InlineData("google")]
    [InlineData("Google")]
    [InlineData("GOOGLE")]
    public void ResolveProvider_WithConfiguredProvider_ShouldReturnProvider(string provider)
    {
        Result<ExternalProviderModel> result = IdentityEndpoint.ResolveProvider(
            Providers(),
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(IdentityConstant.Providers.Google);
    }

    [Theory(DisplayName = "The openid route alias should resolve to OpenID Connect")]
    [InlineData("openid")]
    [InlineData("openidconnect")]
    [InlineData("OpenIdConnect")]
    public void ResolveProvider_WithOpenIdConnectAlias_ShouldReturnProvider(string provider)
    {
        Result<ExternalProviderModel> result = IdentityEndpoint.ResolveProvider(
            Providers(),
            provider);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(IdentityConstant.Providers.OpenIdConnect);
    }

    [Theory(DisplayName = "An unknown or missing provider should produce a 404 failure")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("github")]
    public void ResolveProvider_WithUnknownProvider_ShouldReturn404(string? provider)
    {
        Result<ExternalProviderModel> result = IdentityEndpoint.ResolveProvider(
            Providers(),
            provider);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.ProviderUnsupported");
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
        string segment = IdentityEndpoint.ProviderSegment(scheme);

        segment.Should().Be(expectedSegment);
        IdentityEndpoint.CallbackPath(segment).Should().Be(expectedPath);
    }

    [Theory(DisplayName = "The challenge should use the handler's configured callback path")]
    [InlineData("Google", "/api/auth/external/google/callback")]
    [InlineData("Microsoft", "/api/auth/external/microsoft/callback")]
    public void CallbackRedirectUri_ForOAuthProvider_ShouldUseTheRoute(
        string scheme,
        string expected)
    {
        var provider = new ExternalProviderModel(scheme, scheme);

        IdentityEndpoint.CallbackRedirectUri(provider, new ExternalAuthOption())
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

        IdentityEndpoint.CallbackRedirectUri(provider, option)
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

        IdentityEndpoint.CallbackRedirectUri(provider, option)
            .Should().Be("/api/auth/external/openid/callback");
    }

    [Fact(DisplayName = "External claims should map to the normalized user information")]
    public void ToExternalUserInfo_WithMappedClaims_ShouldReturnUser()
    {
        var principal = Principal(
            new Claim(ClaimTypes.NameIdentifier, "provider-key"),
            new Claim(ClaimTypes.Name, "External User"),
            new Claim(ClaimTypes.Email, "external@example.com"));

        Result<ExternalUserInfo> result = IdentityEndpoint.ToExternalUserInfo(
            IdentityConstant.Providers.Google,
            principal);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new ExternalUserInfo(
            IdentityConstant.Providers.Google,
            "provider-key",
            "External User",
            "external@example.com"));
    }

    [Fact(DisplayName = "Raw OIDC sub and email claims should map to the normalized user information")]
    public void ToExternalUserInfo_WithRawClaims_ShouldReturnUser()
    {
        var principal = Principal(
            new Claim("sub", "provider-key"),
            new Claim("name", "External User"),
            new Claim("email", "external@example.com"));

        Result<ExternalUserInfo> result = IdentityEndpoint.ToExternalUserInfo(
            IdentityConstant.Providers.OpenIdConnect,
            principal);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderKey.Should().Be("provider-key");
        result.Value.DisplayName.Should().Be("External User");
        result.Value.Email.Should().Be("external@example.com");
    }

    [Fact(DisplayName = "A principal without a subject claim should produce a 401 failure")]
    public void ToExternalUserInfo_WithoutSubject_ShouldReturn401()
    {
        Result<ExternalUserInfo> result = IdentityEndpoint.ToExternalUserInfo(
            IdentityConstant.Providers.Google,
            Principal(new Claim(ClaimTypes.Email, "external@example.com")));

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.SubjectMissing");
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
        IdentityEndpoint.BuildRedirectUrl(redirectUrl, "abc").Should().Be(expected);
    }

    [Fact(DisplayName = "Provider helpers should reject missing inputs")]
    public void ProviderHelpers_WithMissingInputs_ShouldThrow()
    {
        Action nullProviders = () => IdentityEndpoint.ResolveProvider(null!, "google");
        Action nullSegment = () => IdentityEndpoint.ProviderSegment(null!);
        Action blankSegment = () => IdentityEndpoint.ProviderSegment(" ");
        Action nullCallback = () => IdentityEndpoint.CallbackPath(string.Empty);

        nullProviders.Should().Throw<ArgumentNullException>();
        nullSegment.Should().Throw<ArgumentException>();
        blankSegment.Should().Throw<ArgumentException>();
        nullCallback.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "External claim mapping should reject a missing principal")]
    public void ToExternalUserInfo_WithoutPrincipal_ShouldThrow()
    {
        Action act = () => IdentityEndpoint.ToExternalUserInfo(
            IdentityConstant.Providers.Google,
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The redirect builder should reject a missing code")]
    public void BuildRedirectUrl_WithoutCode_ShouldThrow()
    {
        Action act = () => IdentityEndpoint.BuildRedirectUrl("/", " ");

        act.Should().Throw<ArgumentException>();
    }

    private static IReadOnlyCollection<ExternalProviderModel> Providers() =>
        [
            new(IdentityConstant.Providers.Google, "Google"),
            new(IdentityConstant.Providers.OpenIdConnect, "OpenID Connect")
        ];

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));

    private static async Task<(int StatusCode, string Body)> ExecuteAsync(IResult result)
    {
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);

        body.Position = 0;
        using var reader = new StreamReader(body);

        return (context.Response.StatusCode, await reader.ReadToEndAsync(
            TestContext.Current.CancellationToken));
    }

    private sealed class StubUserContext(string? userId) : IUserContext
    {
        public string? UserId => userId;

        public string? UserName => null;

        public string? Email => null;

        public bool IsAuthenticated => userId is not null;

        public string? IpAddress => null;

        public string? Device => null;
    }
}
