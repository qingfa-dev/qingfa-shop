using System.Net;
using System.Text;

using AwesomeAssertions;

namespace AppHost.Integration.Tests;

/// <summary>Proves the payments routes reach the Api with the right edge policy.</summary>
[Collection(AppHostCollection.Name)]
public sealed class GatewayPaymentsSpec(AppHostFixture fixture)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(180);

    [Fact]
    public async Task WebhookRoute_WithoutToken_ShouldReachTheApi()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await fixture.GetAppAsync();
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("gateway")
            .WaitAsync(Timeout);

        using var client = app.CreateHttpClient("gateway", "http");
        using var content = new StringContent(
            """{"id":"evt_1","type":"captured","reference":"fake_1"}""",
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await client
            .PostAsync("/api/payments/webhooks/fake", content)
            .WaitAsync(Timeout);

        // Validate: reaching the Api at all proves the route; a missing signature is then
        // rejected by the endpoint itself.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaymentsRoute_WithoutToken_ShouldReturn401()
    {
        JavaScriptResources.SkipUnlessToolchainAvailable();

        var app = await fixture.GetAppAsync();
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("gateway")
            .WaitAsync(Timeout);

        using var client = app.CreateHttpClient("gateway", "http");

        using HttpResponseMessage response = await client
            .GetAsync("/api/payments")
            .WaitAsync(Timeout);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
