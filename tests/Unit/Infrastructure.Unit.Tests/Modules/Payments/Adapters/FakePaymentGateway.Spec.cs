using System.Security.Cryptography;
using System.Text;

using Application.Modules.Payments.Ports.Models;

using AwesomeAssertions;

using Core.BuildingBlocks.Domain.ValueObjects.Money;
using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments.Adapters.Fake;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class FakePaymentGatewaySpec
{
    private static readonly Money Amount = new(25m, new Currency("USD"));

    private static PaymentIntentRequest Request(string externalReference) => new(
        Guid.NewGuid(), Amount, PaymentProvider.Fake, Guid.NewGuid(),
        externalReference, null, null, PaymentCaptureMode.Automatic, "key-1",
        new Dictionary<string, string>());

    [Fact]
    public async Task CreateIntent_DeclinePrefix_ShouldReportFailure()
    {
        var gateway = new FakePaymentGateway();

        var result = await gateway.CreateIntentAsync(Request("decline:card"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentProviderIntentStatus.Failed);
        result.Value.FailureCode.Should().Be("fake_declined");
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
        result.Errors[0].Code.Should().Be("Payments.Provider.Unavailable");
        result.Errors[0].Status.Should().Be(502);
    }

    [Fact]
    public void Resolve_DisabledProvider_ShouldReturn503()
    {
        var registry = new PaymentGatewayRegistry([new FakePaymentGateway()]);

        var result = registry.Resolve(PaymentProvider.Stripe);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Payments.Provider.Disabled");
        result.Errors[0].Status.Should().Be(503);
    }

    [Fact]
    public async Task ParseWebhook_BadSignature_ShouldFail()
    {
        var gateway = new FakePaymentGateway();
        const string body = """{"id":"evt_1","type":"captured","reference":"fake_1"}""";

        var result = await gateway.ParseWebhookAsync(
            body,
            new Dictionary<string, string> { [PaymentConstant.Headers.FakeSignature] = "sha256=bad" },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Payments.Webhook.SignatureInvalid");
    }

    [Fact]
    public async Task ParseWebhook_ValidSignature_ShouldNormalize()
    {
        var gateway = new FakePaymentGateway();
        const string body = """{"id":"evt_1","type":"captured","reference":"fake_1","amount":25.00,"currency":"USD"}""";
        string signature = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{FakePaymentGateway.WebhookSecret}:{body}")));

        var result = await gateway.ParseWebhookAsync(
            body,
            new Dictionary<string, string> { [PaymentConstant.Headers.FakeSignature] = $"sha256={signature}" },
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
            body,
            new Dictionary<string, string> { ["X-Fake-Signature"] = FakePaymentGateway.Sign(body) },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }
}
