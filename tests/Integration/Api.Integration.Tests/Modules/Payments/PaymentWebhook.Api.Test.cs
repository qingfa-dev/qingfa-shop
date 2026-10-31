using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Api.Integration.Tests.BuildingBlocks.Api;

using AwesomeAssertions;

using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments.Adapters.Fake;

namespace Api.Integration.Tests.Modules.Payments;

/// <summary>Exercises the anonymous, signature-verified webhook surface.</summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class PaymentWebhookApiTest(ResultContractStackFixture fixture)
{
    [Fact]
    public async Task Webhook_WithoutSignature_ShouldReturn400()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/payments/webhooks/fake",
            new { id = "evt_1", type = "captured", reference = "fake_1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_UnsupportedEvent_ShouldReturn200()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        const string body = """{"id":"evt_2","type":"dispute_opened","reference":"fake_1"}""";

        using HttpResponseMessage response = await PostSignedAsync(client, body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_MalformedJson_ShouldReturn400()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        const string body = "{not-json";

        using HttpResponseMessage response = await PostSignedAsync(client, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_UnknownProvider_ShouldReturn404()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/payments/webhooks/unknown",
            new { id = "evt_x", type = "captured" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Webhook_Captured_ShouldSettleThePayment()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        Guid id = await CreatePaymentAsync(client, 25m, "cart-hook");

        string body = BuildCapturedEvent(id, 25m);

        using HttpResponseMessage response = await PostSignedAsync(client, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await WaitForStatusAsync(client, id)).Should().Be("captured");
    }

    [Fact]
    public async Task Webhook_Duplicate_ShouldApplyOnce()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        Guid id = await CreatePaymentAsync(client, 30m, "cart-dup");

        string body = BuildCapturedEvent(id, 30m);

        using (HttpResponseMessage first = await PostSignedAsync(client, body))
            first.StatusCode.Should().Be(HttpStatusCode.OK);

        using (HttpResponseMessage second = await PostSignedAsync(client, body))
            second.StatusCode.Should().Be(HttpStatusCode.OK);

        (await WaitForStatusAsync(client, id)).Should().Be("captured");

        using HttpResponseMessage read = await client.GetAsync($"/api/payments/{id}");
        using JsonDocument document = JsonDocument.Parse(await read.Content.ReadAsStringAsync());

        document.RootElement.GetProperty("value").GetProperty("capturedAmount").GetProperty("amount")
            .GetDecimal().Should().Be(30m);
    }

    [Fact]
    public async Task Webhook_AmountMismatch_ShouldNotSettle()
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        Guid id = await CreatePaymentAsync(client, 40m, "cart-mismatch");

        string body = BuildCapturedEvent(id, 50m);

        using HttpResponseMessage response = await PostSignedAsync(client, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpResponseMessage read = await client.GetAsync($"/api/payments/{id}");
        using JsonDocument document = JsonDocument.Parse(await read.Content.ReadAsStringAsync());

        document.RootElement.GetProperty("value").GetProperty("status").GetString()
            .Should().Be("pending");
    }

    /// <summary>Posts a body with the fake provider's signature.</summary>
    private static async Task<HttpResponseMessage> PostSignedAsync(HttpClient client, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhooks/fake")
        {
            Content = new StringContent(body, Encoding.UTF8, PaymentConstant.ContentTypes.Json)
        };

        request.Headers.Add(PaymentConstant.Headers.FakeSignature, FakePaymentGateway.Sign(body));

        return await client.SendAsync(request);
    }

    /// <summary>Creates a payment and returns its id.</summary>
    private static async Task<Guid> CreatePaymentAsync(HttpClient client, decimal amount, string externalReference)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount, currency = "USD" },
                provider = "fake",
                externalReference,
                captureMode = "automatic"
            })
        };

        request.Headers.Add(PaymentConstant.Headers.IdempotencyKey, Guid.NewGuid().ToString());

        using HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("value").GetProperty("id").GetGuid();
    }

    /// <summary>Builds a captured event for the fake provider.</summary>
    private static string BuildCapturedEvent(Guid id, decimal amount) => $$"""
        {"id":"evt_{{Guid.NewGuid():N}}","type":"captured","reference":"fake_{{id:N}}","amount":{{amount}},"currency":"USD"}
        """;

    /// <summary>Waits for the payment to leave the pending state.</summary>
    private static async Task<string> WaitForStatusAsync(HttpClient client, Guid id)
    {
        for (int attempt = 0; attempt < 25; attempt++)
        {
            using HttpResponseMessage read = await client.GetAsync($"/api/payments/{id}");

            if (read.IsSuccessStatusCode)
            {
                using JsonDocument document = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
                string status = document.RootElement.GetProperty("value").GetProperty("status").GetString()!;

                if (status is not "pending")
                    return status;
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException("The payment did not settle in time.");
    }
}
