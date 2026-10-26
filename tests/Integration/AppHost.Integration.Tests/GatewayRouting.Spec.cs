using System.Net.Http.Json;

using AwesomeAssertions;

namespace AppHost.Integration.Tests;

[Collection(AppHostCollection.Name)]
public class GatewayRoutingSpec
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(180);

    private readonly AppHostFixture _fixture;

    public GatewayRoutingSpec(AppHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ApiRoute_WhenCalledThroughGateway_ShouldReachApi()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await _fixture.GetAppAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api")
            .WaitAsync(Timeout);
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("gateway")
            .WaitAsync(Timeout);

        // Initialize: use HTTP so the test does not depend on a trusted dev cert.
        using var httpClient = app.CreateHttpClient("gateway", "http");

        // Validate: the gateway strips the /api prefix, so /api/time must reach the
        // Api's /time endpoint and come back as JSON.
        using var response = await httpClient.GetAsync("/api/time").WaitAsync(Timeout);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<TimePayload>().WaitAsync(Timeout);
        payload.Should().NotBeNull();
        payload!.UtcNow.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task ShopFrontRoute_WhenCalledThroughGateway_ShouldServeHtml()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await _fixture.GetAppAsync();

        // Validate: Vite resources expose no readiness check, so wait for Running
        // and prove reachability over HTTP.
        await app.ResourceNotifications
            .WaitForResourceAsync("shopfront", KnownResourceStates.Running)
            .WaitAsync(Timeout);

        using var httpClient = app.CreateHttpClient("gateway", "http");
        using var response = await httpClient.GetAsync("/").WaitAsync(Timeout);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Validate: the storefront title proves the ShopFront app was served, not
        // the Admin app.
        var html = await response.Content.ReadAsStringAsync().WaitAsync(Timeout);
        html.Should().Contain("<title>QingFa.Shop · Storefront</title>");
    }

    [Fact]
    public async Task AdminRoute_WhenCalledThroughGateway_ShouldServeHtmlUnderAdminBase()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await _fixture.GetAppAsync();

        await app.ResourceNotifications
            .WaitForResourceAsync("admin", KnownResourceStates.Running)
            .WaitAsync(Timeout);

        using var httpClient = app.CreateHttpClient("gateway", "http");

        // Validate: the Admin app is served under a /admin base, and the gateway
        // keeps that prefix rather than stripping it.
        using var response = await httpClient.GetAsync("/admin/").WaitAsync(Timeout);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Validate: the Admin title proves the Admin app was served under /admin,
        // with the prefix preserved rather than stripped.
        var html = await response.Content.ReadAsStringAsync().WaitAsync(Timeout);
        html.Should().Contain("<title>QingFa.Shop · Admin</title>");
    }

    [Fact]
    public async Task GatewayHealth_WhenCalledDirectly_ShouldReturnOk()
    {
        // Guard: the shared AppHost also starts the Vite resources, so this test needs
        // the same toolchain the other AppHost tests do.
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await _fixture.GetAppAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("gateway")
            .WaitAsync(Timeout);

        using var httpClient = app.CreateHttpClient("gateway", "http");

        // Validate: /alive is the gateway's own liveness endpoint and is not routed
        // to the Api, which lives under /api.
        using var response = await httpClient.GetAsync("/alive").WaitAsync(Timeout);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record TimePayload(DateTimeOffset UtcNow);
}