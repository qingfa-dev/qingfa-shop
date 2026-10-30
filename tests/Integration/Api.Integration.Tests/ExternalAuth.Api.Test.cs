using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Application.Common.Security.Authentication.Identity;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Integration.Tests;

/// <summary>Exercises provider discovery, the challenge, and the one-time-code exchange.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class ExternalAuthApiTest
{
    private const string CallbackUrl = "/api/auth/external/google/callback";

    private readonly IdentityApiFactory _factory;

    public ExternalAuthApiTest(IdentityApiFactory factory)
    {
        _factory = factory;
        _factory.Identity.Reset();
        _factory.External.Reset(_factory.Identity.CreateAuthResponse());
    }

    [Fact(DisplayName = "GET /api/auth/providers should return the configured providers")]
    public async Task GetProviders_ShouldReturnConfiguredProviders()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/auth/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(response);
        JsonElement value = document.RootElement.GetProperty("value");

        value.GetArrayLength().Should().Be(1);
        value[0].GetProperty("name").GetString().Should().Be(IdentityConstant.Providers.Google);
        value[0].GetProperty("displayName").GetString().Should().Be("Google");
    }

    [Fact(DisplayName = "GET challenge for an unknown provider should return a Result 404")]
    public async Task Challenge_WithUnknownProvider_ShouldReturn404Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/auth/external/github/challenge");

        await AssertFailureAsync(response, HttpStatusCode.NotFound);
        response.Headers.Location.Should().BeNull();
    }

    [Fact(DisplayName = "GET challenge should redirect to the configured provider")]
    public async Task Challenge_WithConfiguredProvider_ShouldRedirectToProvider()
    {
        using HttpClient client = CreateClientWithoutRedirects();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/auth/external/google/challenge");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("https://provider.example/authorize");
    }

    [Fact(DisplayName = "The callback should store a single-use code and exchange it for tokens")]
    public async Task Callback_ThenExchange_ShouldReturnTokensOnce()
    {
        using HttpClient client = CreateClientWithoutRedirects();

        using HttpResponseMessage callback = await client.GetAsync(
            $"{CallbackUrl}?code=provider-code&state=state");

        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);

        string location = callback.Headers.Location!.ToString();
        location.Should().StartWith("/?code=");
        location.Should().NotContain("access-token");
        location.Should().NotContain("refresh-token");

        string code = location[(location.IndexOf("code=", StringComparison.Ordinal) + 5)..];

        _factory.External.LastUser.Should().BeEquivalentTo(new ExternalUserInfo(
            IdentityConstant.Providers.Google,
            "google-user-1",
            "External User",
            "external@example.com"));

        using HttpResponseMessage exchange = await client.PostAsJsonAsync(
            "/api/auth/external/exchange",
            new { code });

        exchange.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = await ReadJsonAsync(exchange);
        JsonElement value = document.RootElement.GetProperty("value");

        value.GetProperty("accessToken").GetString().Should().Be("access-token");
        value.GetProperty("refreshToken").GetString().Should().Be("refresh-token");

        using HttpResponseMessage replay = await client.PostAsJsonAsync(
            "/api/auth/external/exchange",
            new { code });

        await AssertFailureAsync(replay, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "A rejected provider callback should return a Result 401")]
    public async Task Callback_WhenProviderRejects_ShouldReturn401Result()
    {
        using HttpClient client = CreateClientWithoutRedirects();

        using HttpResponseMessage response = await client.GetAsync($"{CallbackUrl}?fail=true");

        await AssertFailureAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "A callback for an unknown provider should return a Result 404")]
    public async Task Callback_WithUnknownProvider_ShouldReturn404Result()
    {
        using HttpClient client = CreateClientWithoutRedirects();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/auth/external/github/callback");

        await AssertFailureAsync(response, HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "A callback without a subject claim should return a Result 401")]
    public async Task Callback_WithoutSubject_ShouldReturn401Result()
    {
        using HttpClient client = CreateClientWithoutRedirects();

        using HttpResponseMessage response = await client.GetAsync($"{CallbackUrl}?nosub=true");

        await AssertFailureAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "A failed external login should return its Result failure")]
    public async Task Callback_WhenLoginFails_ShouldPreserveFailure()
    {
        _factory.External.LoginHandler = _ => Result<AuthResponse>.Fail(Error.Forbidden(
            "Test.ExternalDenied",
            "The external account is not allowed."));

        using HttpClient client = CreateClientWithoutRedirects();

        using HttpResponseMessage response = await client.GetAsync(CallbackUrl);

        await AssertFailureAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST exchange with an unknown code should return a Result 401")]
    public async Task Exchange_WithUnknownCode_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/external/exchange",
            new { code = "unknown-code" });

        await AssertFailureAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST exchange without a code should return a Result 401")]
    public async Task Exchange_WithoutCode_ShouldReturn401Result()
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/external/exchange",
            new { });

        await AssertFailureAsync(response, HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClientWithoutRedirects() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

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

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
}
