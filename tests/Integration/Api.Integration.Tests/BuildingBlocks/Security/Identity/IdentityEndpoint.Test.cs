using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

using Application.BuildingBlocks.Security.Authentication.Contexts;

using Core.BuildingBlocks.Functional.Results;

using Microsoft.AspNetCore.Http;

using Application.BuildingBlocks.Security.Identity.Auth;
using Application.BuildingBlocks.Security.Identity.Auth.Commands;
using Application.BuildingBlocks.Security.Identity.Auth.Models;
using Application.BuildingBlocks.Security.Identity.External.Models;

using Infrastructure.BuildingBlocks.Adapters.Serialization;

using Web.Api.Endpoints.Identity;
using Web.Api.Endpoints.Identity.Mappings;
using Web.Api.Endpoints.Identity.Models;


namespace Api.Integration.Tests.BuildingBlocks.Security.Identity;

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

    [Fact(DisplayName = "External claims should map to the normalized user information")]
    public void ToExternalUserInfo_WithMappedClaims_ShouldReturnUser()
    {
        var principal = Principal(
            new Claim(ClaimTypes.NameIdentifier, "provider-key"),
            new Claim(ClaimTypes.Name, "External User"),
            new Claim(ClaimTypes.Email, "external@example.com"));

        Result<ExternalUserInfo> result = ExternalPrincipalMapper.ToExternalUserInfo(
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

        Result<ExternalUserInfo> result = ExternalPrincipalMapper.ToExternalUserInfo(
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
        Result<ExternalUserInfo> result = ExternalPrincipalMapper.ToExternalUserInfo(
            IdentityConstant.Providers.Google,
            Principal(new Claim(ClaimTypes.Email, "external@example.com")));

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Auth.External.SubjectMissing");
    }

    [Fact(DisplayName = "External claim mapping should reject a missing principal")]
    public void ToExternalUserInfo_WithoutPrincipal_ShouldThrow()
    {
        Action act = () => ExternalPrincipalMapper.ToExternalUserInfo(
            IdentityConstant.Providers.Google,
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

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
