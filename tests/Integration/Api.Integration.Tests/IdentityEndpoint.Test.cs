using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

using Application.Common.Security.Authentication.Contexts;

using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Http;

using Web.Api.Endpoints.Identity;
using Web.Api.Endpoints.Results;

namespace Api.Integration.Tests;

/// <summary>Locks the current-user guard and principal-claim helpers used by the identity routes.</summary>
[Trait("Category", "Unit")]
public sealed class IdentityEndpointTest
{
    [Fact(DisplayName = "A valid user identifier should pass through")]
    public void RequireUserId_WithValidIdentifier_ShouldReturnUserId()
    {
        Guid userId = Guid.NewGuid();

        Result<Guid> result = IdentityEndpoint.RequireUserId(
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
        Result<Guid> result = IdentityEndpoint.RequireUserId(new StubUserContext(userId));

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        result.Errors.Should().ContainSingle(
            error => error.Code == "Authentication.UserIdMissing");
    }

    [Fact(DisplayName = "The guarded action should not run when the user identifier is missing")]
    public async Task WithUserIdAsync_WithMissingUserId_ShouldReturn401Result()
    {
        IResult result = await IdentityEndpoint.WithUserIdAsync(
            new StubUserContext(null),
            (_, _) => throw new InvalidOperationException("The action must not run."),
            TestContext.Current.CancellationToken);

        (int statusCode, string body) = await ExecuteAsync(result);

        statusCode.Should().Be(401);

        using JsonDocument document = JsonDocument.Parse(body);

        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().BePositive();
    }

    [Fact(DisplayName = "The guarded action should run with the parsed user identifier")]
    public async Task WithUserIdAsync_WithValidIdentifier_ShouldInvokeAction()
    {
        Guid userId = Guid.NewGuid();
        Guid captured = Guid.Empty;

        IResult result = await IdentityEndpoint.WithUserIdAsync(
            new StubUserContext(userId.ToString()),
            (id, _) =>
            {
                captured = id;
                return Task.FromResult(Result.Ok().ToApiResult());
            },
            TestContext.Current.CancellationToken);

        captured.Should().Be(userId);
        (int statusCode, _) = await ExecuteAsync(result);
        statusCode.Should().Be(200);
    }

    [Fact(DisplayName = "Logout should read the jti and exp claims into the application request")]
    public void ToLogoutRequest_ShouldForwardPrincipalClaims()
    {
        var principal = Principal(
            new Claim(JwtRegisteredClaimNames.Jti, "access-jti"),
            new Claim(JwtRegisteredClaimNames.Exp, "1893456000"));

        var mapped = IdentityEndpoint.ToLogoutRequest(
            new LogoutRequest("refresh-token"),
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

        var mapped = IdentityEndpoint.ToLogoutRequest(null, principal);

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
