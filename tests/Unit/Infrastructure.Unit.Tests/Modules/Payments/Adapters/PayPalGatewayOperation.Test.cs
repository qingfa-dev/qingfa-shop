using System.Net;
using System.Text;

using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments.Adapters.PayPal;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class PayPalGatewayOperationTest
{
    private static readonly PayPalOptions Options = new()
    {
        ClientId = "client",
        ClientSecret = "secret",
        WebhookId = "wh_1"
    };

    private static Money Usd(decimal amount) => new(amount, new Currency("USD"));

    [Fact]
    public async Task CreateIntent_ShouldReturnTheApprovalLink()
    {
        PayPalPaymentGateway gateway = Build("""
            {"id":"ORDER-1","status":"CREATED","links":[{"rel":"approve","href":"https://paypal/approve"}]}
            """);

        var result = await gateway.CreateIntentAsync(Request(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("ORDER-1");
        result.Value.ApprovalUrl.Should().Be("https://paypal/approve");
        result.Value.Status.Should().Be(PaymentProviderIntentStatus.RequiresAction);
    }

    [Fact]
    public async Task CreateIntent_Rejected_ShouldReportRequestRejected()
    {
        PayPalPaymentGateway gateway = Build("{}", HttpStatusCode.BadRequest);

        var result = await gateway.CreateIntentAsync(Request(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ProviderRequestRejected);
    }

    [Theory]
    [InlineData("COMPLETED", PaymentProviderIntentStatus.Captured)]
    [InlineData("APPROVED", PaymentProviderIntentStatus.Authorized)]
    [InlineData("CREATED", PaymentProviderIntentStatus.RequiresAction)]
    [InlineData("VOIDED", PaymentProviderIntentStatus.Failed)]
    public async Task Retrieve_ShouldMapTheOrderStatus(string status, PaymentProviderIntentStatus expected)
    {
        PayPalPaymentGateway gateway = Build($$"""{"id":"ORDER-1","status":"{{status}}"}""");

        var result = await gateway.RetrieveAsync("ORDER-1", CancellationToken.None);

        result.Value.Status.Should().Be(expected);
    }

    [Fact]
    public async Task Capture_Success_ShouldReportTheCaptureReference()
    {
        PayPalPaymentGateway gateway = Build("""
            {"id":"ORDER-1","status":"COMPLETED",
             "purchase_units":[{"payments":{"captures":[{"id":"CAP-1","status":"COMPLETED"}]}}]}
            """);

        var result = await gateway.CaptureAsync(
            new PaymentCaptureRequest("ORDER-1", Usd(10m), "capture:1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
        result.Value.CaptureReference.Should().Be("CAP-1");
    }

    [Fact]
    public async Task Refund_ShouldTargetTheCapture()
    {
        PayPalPaymentGateway gateway = Build("""{"id":"REFUND-1","status":"COMPLETED"}""", capture: "/v2/payments/captures/CAP-1/refund");

        var result = await gateway.RefundAsync(
            new PaymentRefundRequest("ORDER-1", Guid.NewGuid(), Usd(10m), "refund:1", "returned", "CAP-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("REFUND-1");
    }

    [Fact]
    public void Capabilities_ShouldAllowPartialRefundsAndManualCapture()
    {
        PayPalPaymentGateway gateway = Build("{}");

        gateway.Capabilities.SupportsPartialRefund.Should().BeTrue();
        gateway.Capabilities.SupportsManualCapture.Should().BeTrue();
        gateway.Capabilities.SupportsWebhooks.Should().BeTrue();
    }

    [Fact]
    public async Task Refund_PartialAmount_ShouldSucceed()
    {
        PayPalPaymentGateway gateway = Build("""{"id":"REFUND-2","status":"COMPLETED"}""", capture: "/v2/payments/captures/CAP-1/refund");

        var result = await gateway.RefundAsync(
            new PaymentRefundRequest("ORDER-1", Guid.NewGuid(), Usd(4m), "refund:2", "partial", "CAP-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
        result.Value.Amount.Amount.Should().Be(4m);
    }

    [Fact]
    public async Task Refund_WithoutCaptureReference_ShouldFail()
    {
        PayPalPaymentGateway gateway = Build("{}");

        var result = await gateway.RefundAsync(
            new PaymentRefundRequest("ORDER-1", Guid.NewGuid(), Usd(10m), "refund:1", "returned"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ProviderReferenceMissing);
    }

    [Fact]
    public async Task Capture_Rejected_ShouldReportAFailedCapture()
    {
        PayPalPaymentGateway gateway = Build("{}", HttpStatusCode.UnprocessableEntity);

        var result = await gateway.CaptureAsync(
            new PaymentCaptureRequest("ORDER-1", Usd(10m), "capture:1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeFalse();
        result.Value.FailureCode.Should().Be(PaymentConstant.Reasons.CaptureFailed);
    }

    [Fact]
    public async Task Cancel_Success_ShouldReportSuccess()
    {
        PayPalPaymentGateway gateway = Build("{}");

        var result = await gateway.CancelAsync(
            new PaymentCancelRequest("ORDER-1", "cancel:1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Retrieve_NullReference_ShouldStillCallTheProvider()
    {
        PayPalPaymentGateway gateway = Build("""{"id":"ORDER-1","status":"COMPLETED"}""");

        var result = await gateway.RetrieveAsync("ORDER-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>Builds an adapter whose token and order calls are stubbed.</summary>
    private static PayPalPaymentGateway Build(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        string? capture = null)
    {
        var client = new HttpClient(new StubHandler(json, status, capture))
        {
            BaseAddress = new Uri(Options.BaseAddress)
        };

        return new PayPalPaymentGateway(client, new PayPalTokenProvider(client, Options), Options);
    }

    /// <summary>Builds a canonical intent request.</summary>
    private static PaymentIntentRequest Request() => new(
        Guid.NewGuid(),
        Usd(10m),
        PaymentProvider.PayPal,
        Guid.NewGuid(),
        "cart-1",
        "https://shop/return",
        "https://shop/cancel",
        PaymentCaptureMode.Automatic,
        "key-1",
        new Dictionary<string, string>());

    /// <summary>Answers the token request with a fixed token and everything else with the stub.</summary>
    private sealed class StubHandler(string json, HttpStatusCode status, string? capture = null) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            bool isToken = request.RequestUri!.AbsolutePath.EndsWith("v1/oauth2/token", StringComparison.Ordinal);

            if (!isToken && capture is not null
                && !request.RequestUri!.AbsolutePath.EndsWith(capture, StringComparison.Ordinal))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            return Task.FromResult(isToken
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"access_token":"token","expires_in":3600}""",
                        Encoding.UTF8,
                        "application/json")
                }
                : new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
        }
    }

    /// <summary>Encodes a JSON body the way the endpoint delivers it.</summary>
    private static ReadOnlyMemory<byte> Bytes(string body) => Encoding.UTF8.GetBytes(body);
}
