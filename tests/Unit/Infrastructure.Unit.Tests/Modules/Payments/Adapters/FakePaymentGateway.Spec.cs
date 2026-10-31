using System.Security.Cryptography;
using System.Text;

using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments;
using Infrastructure.Modules.Payments.Adapters.Fake;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class FakePaymentGatewaySpec
{
    private static readonly Money Amount = new(25m, new Currency("USD"));

    private static PaymentIntentRequest Request(string externalReference) => new(
        Guid.NewGuid(), Amount, PaymentProvider.Fake, Guid.NewGuid(),
        externalReference, null, null, PaymentCaptureMode.Automatic, "key-1",
        new Dictionary<string, string>());

    private static IReadOnlyDictionary<string, string> Signed(string body)
        => new Dictionary<string, string>
        {
            [PaymentConstant.Headers.FakeSignature] = FakePaymentGateway.Sign(body)
        };

    [Fact]
    public async Task CreateIntent_DeclinePrefix_ShouldReportFailure()
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.CreateIntentAsync(Request("decline:card"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentProviderIntentStatus.Failed);
        result.Value.FailureCode.Should().Be(FakePaymentGateway.DeclinedCode);
    }

    [Fact]
    public async Task CreateIntent_Default_ShouldReturnApprovalUrl()
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.CreateIntentAsync(Request("cart-1"), CancellationToken.None);

        result.Value.Status.Should().Be(PaymentProviderIntentStatus.RequiresAction);
        result.Value.ApprovalUrl.Should().StartWith("https://fake.pay/");
    }

    [Fact]
    public async Task CreateIntent_TimeoutPrefix_ShouldReportUnavailable()
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.CreateIntentAsync(Request("timeout:card"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ProviderUnavailable);
        result.Errors[0].Status.Should().Be(502);
    }

    [Fact]
    public async Task Retrieve_ShouldReportCapturedReference()
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.RetrieveAsync("fake_1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("fake_1");
        result.Value.Status.Should().Be(PaymentProviderIntentStatus.Captured);
    }

    [Fact]
    public async Task Capture_ShouldReflectIdempotencyKey()
    {
        var gateway = new FakePaymentGateway();
        var request = new PaymentCaptureRequest("fake_1", Amount, "key-1");

        var result = await gateway.CaptureAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be("cap_key-1");
        result.Value.CapturedAmount.Should().Be(Amount);
        result.Value.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_ShouldSucceed()
    {
        var gateway = new FakePaymentGateway();
        var request = new PaymentCancelRequest("fake_1", "key-1");

        var result = await gateway.CancelAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Succeeded.Should().BeTrue();
        result.Value.FailureCode.Should().BeNull();
    }

    [Fact]
    public async Task Refund_ShouldUseRefundIdReference()
    {
        var gateway = new FakePaymentGateway();
        Guid refundId = Guid.NewGuid();
        var request = new PaymentRefundRequest("fake_1", refundId, Amount, "key-1", "customer_request");

        var result = await gateway.RefundAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderReference.Should().Be($"re_{refundId:N}");
        result.Value.Amount.Should().Be(Amount);
        result.Value.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Resolve_EnabledProvider_ShouldReturnGateway()
    {
        var gateway = new FakePaymentGateway();
        var registry = new PaymentGatewayRegistry([gateway]);

        var result = registry.Resolve(PaymentProvider.Fake);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(gateway);
    }

    [Fact]
    public void Resolve_DisabledProvider_ShouldReturn503()
    {
        var registry = new PaymentGatewayRegistry([new FakePaymentGateway()]);

        var result = registry.Resolve(PaymentProvider.Stripe);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.ProviderDisabled);
        result.Errors[0].Status.Should().Be(503);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    [InlineData("[]")]
    [InlineData("\"captured\"")]
    public async Task ParseWebhook_MalformedJson_ShouldFail(string body)
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.ParseWebhookAsync(Bytes(body), Signed(body), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookPayloadInvalid);
    }

    [Theory]
    [InlineData("""{"id":"evt_1","reference":"fake_1"}""")]
    [InlineData("""{"type":"captured","reference":"fake_1"}""")]
    [InlineData("""{"id":"evt_1","type":"captured"}""")]
    [InlineData("""{"id":"evt_1","type":"captured","reference":"fake_1","amount":"25.00","currency":"USD"}""")]
    public async Task ParseWebhook_MissingOrWrongTypedFields_ShouldFail(string body)
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.ParseWebhookAsync(Bytes(body), Signed(body), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookPayloadInvalid);
    }

    [Theory]
    [InlineData("""{"id":null,"type":"captured","reference":"fake_1"}""")]
    [InlineData("""{"id":"evt_1","type":null,"reference":"fake_1"}""")]
    [InlineData("""{"id":"evt_1","type":"captured","reference":null}""")]
    [InlineData("""{"id":"evt_1","type":"captured","reference":"fake_1","amount":null}""")]
    [InlineData("""{"id":"evt_1","type":"captured","reference":"fake_1","amount":25.00,"currency":null}""")]
    public async Task ParseWebhook_ExplicitNulls_ShouldFail(string body)
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.ParseWebhookAsync(Bytes(body), Signed(body), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookPayloadInvalid);
    }

    [Fact]
    public async Task ParseWebhook_BadSignature_ShouldFail()
    {
        var gateway = new FakePaymentGateway();
        const string body = """{"id":"evt_1","type":"captured","reference":"fake_1"}""";

        var result = await gateway.ParseWebhookAsync(
            Bytes(body),
            new Dictionary<string, string>
            {
                [PaymentConstant.Headers.FakeSignature] = $"{FakePaymentGateway.SignaturePrefix}bad"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookSignatureInvalid);
    }

    [Fact]
    public async Task ParseWebhook_ValidSignature_ShouldNormalize()
    {
        var gateway = new FakePaymentGateway();
        const string body = """{"id":"evt_1","type":"captured","reference":"fake_1","amount":25.00,"currency":"USD"}""";
        string signature = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{FakePaymentGateway.WebhookSecret}:{body}")));

        var result = await gateway.ParseWebhookAsync(
            Bytes(body),
            new Dictionary<string, string>
            {
                [PaymentConstant.Headers.FakeSignature] = $"{FakePaymentGateway.SignaturePrefix}{signature}"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Type.Should().Be(PaymentProviderEventType.Captured);
        result.Value.Value.ProviderReference.Should().Be("fake_1");
    }

    [Fact]
    public async Task ParseWebhook_UnsupportedType_ShouldBeEmpty()
    {
        var gateway = new FakePaymentGateway();
        const string body = """{"id":"evt_9","type":"dispute_opened","reference":"fake_1"}""";

        var result = await gateway.ParseWebhookAsync(
            Bytes(body),
            new Dictionary<string, string>
            {
                [PaymentConstant.Headers.FakeSignature] = FakePaymentGateway.Sign(body)
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public async Task ParseWebhook_EmptyBody_ShouldReportAnInvalidPayload()
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.ParseWebhookAsync(
            ReadOnlyMemory<byte>.Empty,
            Signed(string.Empty),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookPayloadInvalid);
    }

    [Fact]
    public async Task ParseWebhook_NullHeaders_ShouldThrow()
    {
        var gateway = new FakePaymentGateway();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => gateway.ParseWebhookAsync(Bytes("{}"), null!, CancellationToken.None));
    }

    [Fact]
    public async Task Retrieve_NullReference_ShouldThrow()
    {
        var gateway = new FakePaymentGateway();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => gateway.RetrieveAsync(null!, CancellationToken.None));
    }

    /// <summary>Encodes a JSON body the way the endpoint delivers it.</summary>
    private static ReadOnlyMemory<byte> Bytes(string body) => Encoding.UTF8.GetBytes(body);
}
