using System.Net;
using System.Text;

using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;

using Infrastructure.Modules.Payments.Adapters.Stripe;

using Stripe;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class StripeGatewayOperationTest
{
    private static readonly StripeOptions Options = new()
    {
        SecretKey = "sk_test_123",
        WebhookSecret = "whsec_test_123"
    };

    private static Money Usd(decimal amount) => new(amount, new Currency("USD"));

    [Fact]
    public async Task CreateIntent_ShouldMapRequiresAction()
    {
        StripePaymentGateway gateway = Build("""
            {"id":"pi_1","object":"payment_intent","status":"requires_action","client_secret":"cs_1","amount":1000,"currency":"usd","created":1700000000}
            """);

        var result = await gateway.CreateIntentAsync(Request(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("pi_1");
        result.Value.Status.Should().Be(PaymentProviderIntentStatus.RequiresAction);
        result.Value.ClientSecret.Should().Be("cs_1");
    }

    [Fact]
    public async Task CreateIntent_WithTooManyDecimals_ShouldFailBeforeCallingStripe()
    {
        StripePaymentGateway gateway = Build("{}");

        var result = await gateway.CreateIntentAsync(
            Request(Usd(10.001m)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.AmountInvalid);
    }

    [Fact]
    public async Task Retrieve_ShouldMapSucceeded()
    {
        StripePaymentGateway gateway = Build("""
            {"id":"pi_1","object":"payment_intent","status":"succeeded","amount":1000,"currency":"usd","created":1700000000}
            """);

        var result = await gateway.RetrieveAsync("pi_1", CancellationToken.None);

        result.Value.Status.Should().Be(PaymentProviderIntentStatus.Captured);
    }

    [Fact]
    public async Task Capture_ShouldReturnTheCapturedAmount()
    {
        StripePaymentGateway gateway = Build("""
            {"id":"pi_1","object":"payment_intent","status":"succeeded","amount":1000,"currency":"usd","created":1700000000}
            """);

        var result = await gateway.CaptureAsync(
            new PaymentCaptureRequest("pi_1", Usd(10m), "capture:1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
        result.Value.CapturedAmount.Amount.Should().Be(10m);
    }

    [Fact]
    public async Task Cancel_ShouldSucceed()
    {
        StripePaymentGateway gateway = Build("""
            {"id":"pi_1","object":"payment_intent","status":"canceled","amount":1000,"currency":"usd","created":1700000000}
            """);

        var result = await gateway.CancelAsync(
            new PaymentCancelRequest("pi_1", "cancel:1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Refund_ShouldReturnTheProviderReference()
    {
        StripePaymentGateway gateway = Build("""
            {"id":"re_1","object":"refund","amount":1000,"currency":"usd","status":"succeeded"}
            """);

        var result = await gateway.RefundAsync(
            new PaymentRefundRequest("pi_1", Guid.NewGuid(), Usd(10m), "refund:1", "returned"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("re_1");
    }

    [Fact]
    public async Task Refund_WithTooManyDecimals_ShouldFail()
    {
        StripePaymentGateway gateway = Build("{}");

        var result = await gateway.RefundAsync(
            new PaymentRefundRequest("pi_1", Guid.NewGuid(), Usd(1.001m), "refund:1", "returned"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CreateIntent_NullRequest_ShouldThrow()
    {
        StripePaymentGateway gateway = Build("{}");

        Func<Task> act = () => gateway.CreateIntentAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>Builds an adapter whose HTTP calls return the supplied JSON.</summary>
    private static StripePaymentGateway Build(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var httpClient = new HttpClient(new JsonHandler(json, status));

        return new StripePaymentGateway(
            Options,
            new StripeClient(Options.SecretKey, httpClient: new SystemNetHttpClient(httpClient)));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, PaymentsOutcome.Codes.ProviderConfigurationInvalid)]
    [InlineData(HttpStatusCode.Forbidden, PaymentsOutcome.Codes.ProviderConfigurationInvalid)]
    [InlineData(HttpStatusCode.BadRequest, PaymentsOutcome.Codes.ProviderRequestRejected)]
    [InlineData(HttpStatusCode.InternalServerError, PaymentsOutcome.Codes.ProviderRequestRejected)]
    public async Task CreateIntent_ProviderFailure_ShouldMapToTheOutcome(
        HttpStatusCode status,
        string expectedCode)
    {
        StripePaymentGateway gateway = Build(
            """{"error":{"type":"invalid_request_error","message":"nope"}}""",
            status);

        var result = await gateway.CreateIntentAsync(Request(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact]
    public async Task Refund_ProviderFailure_ShouldMapToTheOutcome()
    {
        StripePaymentGateway gateway = Build(
            """{"error":{"type":"invalid_request_error","message":"nope"}}""",
            HttpStatusCode.BadRequest);

        var result = await gateway.RefundAsync(
            new PaymentRefundRequest("pi_1", Guid.NewGuid(), Usd(10m), "refund:1", "returned"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ProviderRequestRejected);
    }

    /// <summary>Builds a canonical intent request.</summary>
    private static PaymentIntentRequest Request(Money? amount = null) => new(
        Guid.NewGuid(),
        amount ?? Usd(10m),
        PaymentProvider.Stripe,
        Guid.NewGuid(),
        "cart-1",
        "https://shop/return",
        "https://shop/cancel",
        PaymentCaptureMode.Automatic,
        "key-1",
        new Dictionary<string, string>());

    /// <summary>Answers every Stripe call with the same JSON body and status.</summary>
    private sealed class JsonHandler(string json, HttpStatusCode status) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
