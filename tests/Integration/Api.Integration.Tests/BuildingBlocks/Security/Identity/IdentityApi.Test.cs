using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Application.BuildingBlocks.Security.Authentication.Tokens;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Jwt;

using Core.BuildingBlocks.Functional.Errors;
using Core.BuildingBlocks.Functional.Results;

using Microsoft.Extensions.DependencyInjection;

namespace Api.Integration.Tests.BuildingBlocks.Security.Identity;

/// <summary>Exercises the local identity routes without touching persistence.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class IdentityApiTest
{
    private readonly IdentityApiFactory _factory;

    public IdentityApiTest(IdentityApiFactory factory)
    {
        _factory = factory;
        _factory.Identity.Reset();
    }

    [Fact(DisplayName = "GET /api/auth/me without a token should return a Result 401")]
    public async Task GetMe_WithoutToken_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        await AssertFailureAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /api/auth/logout without a token should return a Result 401")]
    public async Task Logout_WithoutToken_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = "refresh-token" });

        await AssertFailureAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /api/auth/register with a weak password should return 400")]
    public async Task Register_WithWeakPassword_ShouldReturn400Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName = "new-user",
                email = "new@example.com",
                password = "short"
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Password.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/register with a short username should return 400")]
    public async Task Register_WithShortUserName_ShouldReturn400Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName = "ab",
                email = "new@example.com",
                password = "Password@123"
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Operation.Failed");
    }

    [Fact(DisplayName = "POST /api/auth/register with a malformed email should return 401")]
    public async Task Register_WithMalformedEmail_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName = "new-user",
                email = "not-an-email",
                password = "Password@123"
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/login with an empty password should return 401")]
    public async Task Login_WithEmptyPassword_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = "admin", password = string.Empty });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/login with an empty identifier should return 401")]
    public async Task Login_WithEmptyIdentifier_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = string.Empty, password = "Password@123" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/register should return 200 with the created user")]
    public async Task Register_ShouldReturn200ResultValue()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName = "new-user",
                email = "new@example.com",
                password = "Password@123"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Location.Should().BeNull();

        using JsonDocument document = await ReadJsonAsync(response);
        JsonElement value = document.RootElement.GetProperty("value");

        value.GetProperty("id").GetGuid().Should().Be(_factory.Identity.User.Id);
        value.GetProperty("userName").GetString().Should().Be("admin");
    }

    [Fact(DisplayName = "POST /api/auth/login should return 200 with the public token pair")]
    public async Task Login_ShouldReturn200TokenResponse()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = "admin", password = "Password@123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(response);
        JsonElement value = document.RootElement.GetProperty("value");

        value.GetProperty("accessToken").GetString().Should().Be("access-token");
        value.GetProperty("refreshToken").GetString().Should().Be("refresh-token");
        value.GetProperty("user").GetProperty("userName").GetString().Should().Be("admin");
    }

    [Fact(DisplayName = "POST /api/auth/refresh should return 200 with the public token pair")]
    public async Task Refresh_ShouldReturn200TokenResponse()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = "refresh-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(response);
        JsonElement value = document.RootElement.GetProperty("value");

        value.GetProperty("accessToken").GetString().Should().Be("access-token");
        value.GetProperty("refreshToken").GetString().Should().Be("refresh-token");
    }

    [Fact(DisplayName = "POST /api/auth/logout should return 204 and forward principal claims")]
    public async Task Logout_ShouldReturn204AndForwardPrincipalClaims()
    {
        using HttpClient client = CreateAuthenticatedClient(out AccessToken token);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = "logout-refresh" });

        await AssertNoContentAsync(response);

        _factory.Identity.LastLogout.Should().NotBeNull();
        _factory.Identity.LastLogout!.RefreshToken.Should().Be("logout-refresh");
        _factory.Identity.LastLogout.AccessTokenJti.Should().Be(token.Jti);
        _factory.Identity.LastLogout.AccessTokenExpiresAtUtc!.Value
            .ToUnixTimeSeconds()
            .Should()
            .Be(token.ExpiresAtUtc.ToUnixTimeSeconds());
    }

    [Fact(DisplayName = "POST /api/auth/logout should accept an empty body")]
    public async Task Logout_WithEmptyBody_ShouldReturn204()
    {
        using HttpClient client = CreateAuthenticatedClient(out AccessToken token);

        using HttpResponseMessage response = await client.PostAsync("/api/auth/logout", null);

        await AssertNoContentAsync(response);

        _factory.Identity.LastLogout.Should().NotBeNull();
        _factory.Identity.LastLogout!.RefreshToken.Should().BeNull();
        _factory.Identity.LastLogout.AccessTokenJti.Should().Be(token.Jti);
    }

    [Fact(DisplayName = "POST /api/auth/logout should preserve a service failure")]
    public async Task Logout_WhenServiceFails_ShouldReturnResultFailure()
    {
        _factory.Identity.LogoutHandler = _ => Result.Fail(
            Error.BadRequest("Identity.Logout.Failed", "Logout failed."));

        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = "logout-refresh" });

        await AssertFailureAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /api/auth/confirm-email should return 204")]
    public async Task ConfirmEmail_ShouldReturn204()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = Guid.NewGuid().ToString(), token = "confirmation-token" });

        await AssertNoContentAsync(response);
    }

    [Fact(DisplayName = "POST /api/auth/confirm-email with an invalid user id should return 400")]
    public async Task ConfirmEmail_WithInvalidUserId_ShouldReturn400Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = "not-a-guid", token = "confirmation-token" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Token.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/confirm-email with an empty token should return 400")]
    public async Task ConfirmEmail_WithEmptyToken_ShouldReturn400Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = Guid.NewGuid().ToString(), token = string.Empty });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Token.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/resend-confirmation should return 204")]
    public async Task ResendConfirmation_ShouldReturn204()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/resend-confirmation",
            new { email = "admin@example.com" });

        await AssertNoContentAsync(response);
    }

    [Fact(DisplayName = "POST /api/auth/resend-confirmation with a malformed email should return 401")]
    public async Task ResendConfirmation_WithMalformedEmail_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/resend-confirmation",
            new { email = "not-an-email" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/forgot-password should return 204")]
    public async Task ForgotPassword_ShouldReturn204()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = "admin@example.com" });

        await AssertNoContentAsync(response);
    }

    [Fact(DisplayName = "POST /api/auth/forgot-password with a malformed email should return 401")]
    public async Task ForgotPassword_WithMalformedEmail_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = "not-an-email" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/forgot-password should preserve a service failure")]
    public async Task ForgotPassword_WhenServiceFails_ShouldReturnResultFailure()
    {
        _factory.Identity.ForgotPasswordHandler = _ => Result.Fail(
            Error.UnprocessableEntity("Identity.Email.Invalid", "The email is invalid."));

        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = "admin@example.com" });

        await AssertFailureAsync(response, HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "POST /api/auth/reset-password should return 204")]
    public async Task ResetPassword_ShouldReturn204()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new
            {
                email = "admin@example.com",
                token = "reset-token",
                newPassword = "Password@123"
            });

        await AssertNoContentAsync(response);
    }

    [Fact(DisplayName = "POST /api/auth/reset-password with an empty token should return 400")]
    public async Task ResetPassword_WithEmptyToken_ShouldReturn400Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new
            {
                email = "admin@example.com",
                token = string.Empty,
                newPassword = "Password@123"
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Token.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/reset-password with a malformed email should return 401")]
    public async Task ResetPassword_WithMalformedEmail_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new
            {
                email = "not-an-email",
                token = "reset-token",
                newPassword = "Password@123"
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/reset-password with a weak password should return 400")]
    public async Task ResetPassword_WithWeakPassword_ShouldReturn400Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new
            {
                email = "admin@example.com",
                token = "reset-token",
                newPassword = "short"
            });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Password.Invalid");
    }

    [Fact(DisplayName = "GET /api/auth/me should return the current user")]
    public async Task GetMe_ShouldReturn200User()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("value").GetProperty("id").GetGuid()
            .Should().Be(_factory.Identity.User.Id);
    }

    [Fact(DisplayName = "PUT /api/auth/me should forward the update and return the user")]
    public async Task UpdateMe_ShouldReturn200User()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/auth/me",
            new { firstName = "Updated" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Identity.LastUpdateInfo.Should().NotBeNull();
        _factory.Identity.LastUpdateInfo!.FirstName.Should().Be("Updated");

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("value").GetProperty("userName").GetString()
            .Should().Be("admin");
    }

    [Fact(DisplayName = "PUT /api/auth/me with a new password but no old password should return 400")]
    public async Task UpdateMe_WithNewPasswordWithoutOld_ShouldReturn400Result()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/auth/me",
            new { newPassword = "Password@123" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.BadRequest,
            "Identity.Password.CurrentRequired");
    }

    [Fact(DisplayName = "PUT /api/auth/me with a malformed email should return 401")]
    public async Task UpdateMe_WithMalformedEmail_ShouldReturn401Result()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/auth/me",
            new { email = "not-an-email" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "GET /api/auth/me/2fa should return the two-factor state")]
    public async Task GetTwoFactor_ShouldReturn200Info()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.GetAsync("/api/auth/me/2fa");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("value").GetProperty("isTwoFactorEnabled").GetBoolean()
            .Should().BeTrue();
    }

    [Fact(DisplayName = "PUT /api/auth/me/2fa should forward the update and return the state")]
    public async Task UpdateTwoFactor_ShouldReturn200Info()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/auth/me/2fa",
            new { enable = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Identity.LastTwoFactor.Should().NotBeNull();
        _factory.Identity.LastTwoFactor!.Enable.Should().BeFalse();
    }

    [Fact(DisplayName = "PUT /api/auth/me/2fa enabling without a code should return 401")]
    public async Task UpdateTwoFactor_EnablingWithoutCode_ShouldReturn401Result()
    {
        using HttpClient client = CreateAuthenticatedClient(out _);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/auth/me/2fa",
            new { enable = true });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.TwoFactor.Required");
    }

    private HttpClient CreateAuthenticatedClient(out AccessToken accessToken)
    {
        _factory.Clock.SetUtcNow(DateTimeOffset.UtcNow);

        ITokenService tokenService = _factory.Services.GetRequiredService<ITokenService>();
        Result<AccessToken> issued = tokenService.CreateAccessToken(new AccessTokenRequest(
            UserId: _factory.Identity.User.Id.ToString(),
            UserName: _factory.Identity.User.UserName,
            Email: _factory.Identity.User.Email));

        issued.IsSuccess.Should().BeTrue();
        accessToken = issued.Value;

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken.Token);

        return client;
    }

    private static async Task AssertFailureAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        response.StatusCode.Should().Be(expectedStatus);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().BePositive();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
    }

    private static async Task AssertFailureCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        await AssertFailureAsync(response, expectedStatus);

        using JsonDocument document = await ReadJsonAsync(response);

        document.RootElement.GetProperty("errors")[0]
            .GetProperty("code").GetString().Should().Be(expectedCode);
    }

    private static async Task AssertNoContentAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        body.Should().BeEmpty();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }
}
