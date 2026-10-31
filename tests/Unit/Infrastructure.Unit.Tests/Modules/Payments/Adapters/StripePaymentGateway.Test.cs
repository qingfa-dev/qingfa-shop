using System.Text;

using AwesomeAssertions;

using Core.Modules.Payments;
using Core.Modules.Payments.Intents;

using Infrastructure.Modules.Payments.Adapters.Stripe;

using Stripe;

namespace Infrastructure.Unit.Tests.Modules.Payments.Adapters;

public sealed class StripePaymentGatewayTest
{
    private static readonly StripeOptions Options = new()
    {
        SecretKey = "sk_test_123",
        WebhookSecret = "whsec_test_123"
    };

    [Fact]
    public async Task ParseWebhook_MissingSignatureHeader_ShouldFail()
    {
        var gateway = new StripePaymentGateway(Options);

        var result = await gateway.ParseWebhookAsync(
            Bytes("""{"id":"evt_1","type":"payment_intent.succeeded"}"""),
            new Dictionary<string, string>(),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookSignatureInvalid);
    }

    [Fact]
    public async Task ParseWebhook_TamperedBody_ShouldFail()
    {
        var gateway = new StripePaymentGateway(Options);
        const string original = """{"id":"evt_1","type":"payment_intent.succeeded"}""";

        string header = BuildSignature(original, Options.WebhookSecret);

        var result = await gateway.ParseWebhookAsync(
            Bytes("""{"id":"evt_1","type":"payment_intent.succeeded","amount":1}"""),
            new Dictionary<string, string> { [PaymentConstant.Headers.StripeSignature] = header },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(PaymentsOutcome.Codes.WebhookSignatureInvalid);
    }

    [Fact]
    public async Task ParseWebhook_NullHeaders_ShouldThrow()
    {
        var gateway = new StripePaymentGateway(Options);

        Func<Task> act = () =>
            gateway.ParseWebhookAsync(Bytes("{}"), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseWebhook_ValidSignatureAndUnsupportedType_ShouldBeEmpty()
    {
        var gateway = new StripePaymentGateway(Options);
        const string body = """{"id":"evt_1","type":"customer.created"}""";

        var result = await gateway.ParseWebhookAsync(
            Bytes(body),
            new Dictionary<string, string> { [PaymentConstant.Headers.StripeSignature] = BuildSignature(body, Options.WebhookSecret) },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Provider_ShouldBeStripe()
        => new StripePaymentGateway(Options).Provider.Should().Be(PaymentProvider.Stripe);

    /// <summary>Builds the Stripe signature header for a body.</summary>
    private static string BuildSignature(string payload, string secret)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signed = $"{timestamp}.{payload}";

        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret));

        string digest = Convert.ToHexStringLower(
            hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signed)));

        return $"t={timestamp},v1={digest}";
    }

    /// <summary>Encodes a JSON body the way the endpoint delivers it.</summary>
    private static ReadOnlyMemory<byte> Bytes(string body) => Encoding.UTF8.GetBytes(body);
}
