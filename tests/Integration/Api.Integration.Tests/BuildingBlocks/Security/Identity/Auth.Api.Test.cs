using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Api.Integration.Tests.BuildingBlocks.Api;

using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Integration.Tests.BuildingBlocks.Security.Identity;

/// <summary>Exercises the real ASP.NET Core Identity flows through the public routes.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class AuthApiTest : IdentityStackTestBase
{
    private const string Password = "Password@123";
    private const string NewPassword = "NewPassword@123";

    public AuthApiTest(ResultContractStackFixture fixture)
        : base(fixture)
    {
    }

    [Fact(DisplayName = "POST /api/auth/login should sign in a seeded account")]
    public async Task Login_WithSeededAdmin_ShouldReturnTokens()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        JsonElement tokens = await LoginAsync(client, "admin", "Admin@123!");

        tokens.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        tokens.GetProperty("refreshToken").GetString().Should().StartWith("rt_");
        tokens.GetProperty("user").GetProperty("userName").GetString().Should().Be("admin");

        string accessToken = tokens.GetProperty("accessToken").GetString()!;
        using HttpClient bearer = CreateBearerClient(accessToken);

        using HttpResponseMessage me = await bearer.GetAsync("/api/auth/me");

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(me)).GetProperty("userName").GetString().Should().Be("admin");
    }

    [Fact(DisplayName = "POST /api/auth/login with a bad password should return 401")]
    public async Task Login_WithBadPassword_ShouldReturn401()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = "admin", password = "WrongPassword@123" });

        await AssertFailureCodeAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Identity.Credentials.Invalid");
    }

    [Fact(DisplayName = "POST /api/auth/refresh should rotate tokens and reject replay")]
    public async Task Refresh_ShouldRotateAndRejectReplay()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = await RegisterAsync(client);
        JsonElement tokens = await LoginAsync(client, userName, Password);
        string refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        using HttpResponseMessage rotated = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });

        rotated.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement next = await ReadValueAsync(rotated);

        next.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
        next.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();

        using HttpResponseMessage replay = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });

        await AssertFailureAsync(replay, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /api/auth/refresh with an unknown token should return 401")]
    public async Task Refresh_WithUnknownToken_ShouldReturn401()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = "rt_unknown" });

        await AssertFailureAsync(response, HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "POST /api/auth/logout should revoke the refresh token")]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        SkipWithoutContainer();

        using HttpClient anonymous = CreateClient();
        string userName = await RegisterAsync(anonymous);
        JsonElement tokens = await LoginAsync(anonymous, userName, Password);
        string accessToken = tokens.GetProperty("accessToken").GetString()!;
        string refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        using HttpClient bearer = CreateBearerClient(accessToken);

        using HttpResponseMessage logout = await bearer.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken });

        await AssertNoContentAsync(logout);

        using HttpResponseMessage refresh = await anonymous.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });

        await AssertFailureAsync(refresh, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "PUT /api/auth/me should update the current profile")]
    public async Task UpdateMe_ShouldReturnUpdatedProfile()
    {
        SkipWithoutContainer();

        using HttpClient anonymous = CreateClient();
        string userName = await RegisterAsync(anonymous);
        JsonElement tokens = await LoginAsync(anonymous, userName, Password);
        string accessToken = tokens.GetProperty("accessToken").GetString()!;

        using HttpClient bearer = CreateBearerClient(accessToken);

        using HttpResponseMessage response = await bearer.PutAsJsonAsync(
            "/api/auth/me",
            new { firstName = "Updated", lastName = "Profile" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement value = await ReadValueAsync(response);

        value.GetProperty("firstName").GetString().Should().Be("Updated");
        value.GetProperty("lastName").GetString().Should().Be("Profile");
    }

    [Fact(DisplayName = "POST /api/auth/register should confirm the address from the sent link")]
    public async Task RegisterAndConfirmEmail_ShouldLetTheUserSignIn()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = UniqueUserName();
        string email = UniqueEmail(userName);

        using HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { userName, email, password = Password, firstName = "Registered" });

        register.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement user = await ReadValueAsync(register);
        Guid userId = user.GetProperty("id").GetGuid();

        RecordedIdentityEmail? confirmation = Fixture.Factory.Email
            .Latest(email, RecordedIdentityEmailKind.Confirmation);

        confirmation.Should().NotBeNull();

        string token = ReadLinkQuery(confirmation!.Link, "token");
        ReadLinkQuery(confirmation.Link, "userId").Should().Be(userId.ToString());

        using HttpResponseMessage confirm = await client.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { userId = userId.ToString(), token });

        await AssertNoContentAsync(confirm);

        JsonElement tokens = await LoginAsync(client, userName, Password);

        tokens.GetProperty("user").GetProperty("emailConfirmed").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "POST /api/auth/register with a duplicate email should return 409")]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = UniqueUserName();
        string email = UniqueEmail(userName);
        await RegisterAsync(client, userName: userName, email: email);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                userName = UniqueUserName(),
                email,
                password = Password
            });

        await AssertFailureCodeAsync(response, HttpStatusCode.Conflict, "Identity.Email.Taken");
    }

    [Fact(DisplayName = "POST /api/auth/resend-confirmation should record a new link")]
    public async Task ResendConfirmation_ShouldSendAnotherLink()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = UniqueUserName();
        string email = UniqueEmail(userName);
        await RegisterAsync(client, userName: userName, email: email);

        Fixture.Factory.Email.Clear();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/resend-confirmation",
            new { email });

        await AssertNoContentAsync(response);

        Fixture.Factory.Email.Latest(email, RecordedIdentityEmailKind.Confirmation)
            .Should()
            .NotBeNull();
    }

    [Fact(DisplayName = "POST /api/auth/forgot-password and reset-password should change the password")]
    public async Task ForgotAndResetPassword_ShouldChangeThePassword()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = UniqueUserName();
        string email = UniqueEmail(userName);
        await RegisterAsync(client, userName: userName, email: email);
        Fixture.Factory.Email.Clear();

        using HttpResponseMessage forgot = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email });

        await AssertNoContentAsync(forgot);

        RecordedIdentityEmail? message = Fixture.Factory.Email
            .Latest(email, RecordedIdentityEmailKind.PasswordReset);

        message.Should().NotBeNull();

        string token = ReadLinkQuery(message!.Link, "token");

        using HttpResponseMessage reset = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { email, token, newPassword = NewPassword });

        await AssertNoContentAsync(reset);

        JsonElement tokens = await LoginAsync(client, userName, NewPassword);

        tokens.GetProperty("user").GetProperty("userName").GetString().Should().Be(userName);
    }

    [Fact(DisplayName = "POST /api/auth/reset-password with an invalid token should return 400")]
    public async Task ResetPassword_WithInvalidToken_ShouldReturn400()
    {
        SkipWithoutContainer();

        using HttpClient client = CreateClient();
        string userName = UniqueUserName();
        string email = UniqueEmail(userName);
        await RegisterAsync(client, userName: userName, email: email);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { email, token = "invalid-token", newPassword = NewPassword });

        await AssertFailureAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET and PUT /api/auth/me/2fa should manage two-factor authentication")]
    public async Task TwoFactorRoutes_ShouldEnableAndRequireACode()
    {
        SkipWithoutContainer();

        using HttpClient anonymous = CreateClient();
        string userName = await RegisterAsync(anonymous);
        JsonElement tokens = await LoginAsync(anonymous, userName, Password);
        string accessToken = tokens.GetProperty("accessToken").GetString()!;
        Guid userId = tokens.GetProperty("user").GetProperty("id").GetGuid();

        using HttpClient bearer = CreateBearerClient(accessToken);

        using HttpResponseMessage info = await bearer.GetAsync("/api/auth/me/2fa");

        info.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(info)).GetProperty("authenticatorUri").GetString()
            .Should()
            .StartWith("otpauth://totp/");

        using HttpResponseMessage recovery = await bearer.PutAsJsonAsync(
            "/api/auth/me/2fa",
            new { resetRecoveryCodes = true });

        recovery.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(recovery)).GetProperty("recoveryCodes").GetArrayLength().Should().Be(10);

        string code = await GenerateAuthenticatorCodeAsync(userId);

        using HttpResponseMessage enabled = await bearer.PutAsJsonAsync(
            "/api/auth/me/2fa",
            new { enable = true, twoFactorCode = code });

        enabled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(enabled)).GetProperty("isTwoFactorEnabled").GetBoolean().Should().BeTrue();

        using HttpResponseMessage withoutCode = await anonymous.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = userName, password = Password });

        await AssertFailureCodeAsync(
            withoutCode,
            HttpStatusCode.Unauthorized,
            "Identity.TwoFactor.Required");

        using HttpResponseMessage invalidCode = await anonymous.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = userName, password = Password, twoFactorCode = "000000" });

        await AssertFailureCodeAsync(
            invalidCode,
            HttpStatusCode.Unauthorized,
            "Identity.TwoFactor.Invalid");

        using HttpResponseMessage withCode = await anonymous.PostAsJsonAsync(
            "/api/auth/login",
            new { identifier = userName, password = Password, twoFactorCode = code });

        withCode.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpResponseMessage disabled = await bearer.PutAsJsonAsync(
            "/api/auth/me/2fa",
            new { enable = false });

        disabled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadValueAsync(disabled)).GetProperty("isTwoFactorEnabled").GetBoolean().Should().BeFalse();

        using HttpResponseMessage resetKey = await bearer.PutAsJsonAsync(
            "/api/auth/me/2fa",
            new { resetSharedKey = true });

        resetKey.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<string> RegisterAsync(
        HttpClient client,
        string? userName = null,
        string? email = null)
    {
        userName ??= UniqueUserName();
        email ??= UniqueEmail(userName);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { userName, email, password = Password, firstName = "Registered" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return userName;
    }

    private async Task<string> GenerateAuthenticatorCodeAsync(Guid userId)
    {
        using IServiceScope scope = Fixture.Factory.Services.CreateScope();

        UserManager<User> users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        User user = (await users.FindByIdAsync(userId.ToString()))!;
        string key = (await users.GetAuthenticatorKeyAsync(user))!;

        // Compute: the built-in AuthenticatorTokenProvider never generates codes (it only
        // validates), so the test derives the current TOTP from the shared key.
        byte[] secret = FromBase32(key);
        byte[] counter = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(
            counter,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);

        using var hmac = new HMACSHA1(secret);
        byte[] hash = hmac.ComputeHash(counter);
        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] FromBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var bytes = new List<byte>(value.Length * 5 / 8);
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char character in value.TrimEnd('=').ToUpperInvariant())
        {
            int index = alphabet.IndexOf(character, StringComparison.Ordinal);
            if (index < 0)
                continue;

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bytes.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }

        return [.. bytes];
    }
}
