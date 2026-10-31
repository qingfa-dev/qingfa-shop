using System.Net;
using System.Text;
using System.Text.Json;

using AwesomeAssertions;

using Core.Modules.Payments;

using Infrastructure.Modules.Payments.Adapters.PayPal;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class PayPalPaymentGatewayTest
{
    private static readonly PayPalOptions Options = new()
    {
        ClientId = "client",
        ClientSecret = "secret",
        WebhookId = "wh_1",
        Environment = PayPalEnvironment.Sandbox
    };

    [Fact]
    public async Task ParseWebhook_VerificationRejected_ShouldFail()
    {
        PayPalPaymentGateway gateway = Build(Stub(HttpStatusCode.OK, """{"verification_status":"FAILURE"}"""));

        var result = await gateway.ParseWebhookAsync(
            Bytes("""{"id":"evt_1","event_type":"PAYMENT.CAPTURE.COMPLETED","resource":{}}"""),
            new Dictionary<string, string>(),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookSignatureInvalid);
    }

    [Fact]
    public async Task ParseWebhook_UnsupportedEvent_ShouldBeEmpty()
    {
        PayPalPaymentGateway gateway = Build(Stub(HttpStatusCode.OK, """{"verification_status":"SUCCESS"}"""));

        var result = await gateway.ParseWebhookAsync(
            Bytes("""{"id":"evt_1","event_type":"SOMETHING.ELSE","resource":{"id":"x"}}"""),
            new Dictionary<string, string>(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task ParseWebhook_CaptureCompleted_ShouldNormalize()
    {
        PayPalPaymentGateway gateway = Build(Stub(HttpStatusCode.OK, """{"verification_status":"SUCCESS"}"""));

        const string body = """
            {"id":"evt_9","event_type":"PAYMENT.CAPTURE.COMPLETED",
             "resource":{"supplementary_data":{"related_ids":{"order_id":"ORDER-1"}}}}
            """;

        var result = await gateway.ParseWebhookAsync(Bytes(body), new Dictionary<string, string>(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.ProviderReference.Should().Be("ORDER-1");
    }

    [Fact]
    public async Task RefundAsync_ShouldReportPartialUnsupported()
    {
        PayPalPaymentGateway gateway = Build(Stub(HttpStatusCode.OK, "{}"));

        var result = await gateway.RefundAsync(
            new Application.Modules.Payments.Ports.Models.PaymentRefundRequest(
                "ORDER-1",
                Guid.NewGuid(),
                new Core.BuildingBlocks.Domain.ValueObjects.Money.Money(
                    1m,
                    new Core.BuildingBlocks.Domain.ValueObjects.Money.Currency("USD")),
                "refund:1",
                "reason"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.PartialRefundUnsupported);
    }

    [Fact]
    public void Options_LiveEnvironment_ShouldUseTheLiveHost()
        => new PayPalOptions { Environment = PayPalEnvironment.Live }
            .BaseAddress.Should().Be("https://api-m.paypal.com/");

    /// <summary>Builds a gateway whose token endpoint and API share one stub.</summary>
    private static PayPalPaymentGateway Build(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri(Options.BaseAddress) };
        return new PayPalPaymentGateway(client, new PayPalTokenProvider(client, Options), Options);
    }

    /// <summary>Stubs the token endpoint and the verification endpoint.</summary>
    private static HttpMessageHandler Stub(HttpStatusCode status, string body) => new StubHandler(status, body);

    /// <summary>Answers the token request with a fixed token and everything else with the stub.</summary>
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("v1/oauth2/token", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"access_token":"token","expires_in":3600}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            _ = JsonDocument.Parse(body);

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>Encodes a JSON body the way the endpoint delivers it.</summary>
    private static ReadOnlyMemory<byte> Bytes(string body) => Encoding.UTF8.GetBytes(body);
}
