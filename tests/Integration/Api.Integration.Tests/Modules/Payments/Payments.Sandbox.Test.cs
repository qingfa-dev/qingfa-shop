using System.Net;
using System.Net.Http.Json;

using Api.Integration.Tests.BuildingBlocks.Api;

using AwesomeAssertions;

using Core.Modules.Payments;

using Infrastructure.Modules.Payments;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Api.Integration.Tests.Modules.Payments;

/// <summary>
/// Opt-in checks against a real provider sandbox. Skipped unless the provider secrets are
/// present, so CI never needs them. Run with <c>make test.payments.sandbox</c>.
/// </summary>
[Collection(ResultContractStackCollection.Name)]
public sealed class PaymentsSandboxTest(ResultContractStackFixture fixture)
{
    /// <summary>The environment variable that carries a Stripe test key.</summary>
    private const string StripeKeyVariable = "STRIPE__SECRETKEY";

    /// <summary>The environment variable that carries the Stripe webhook secret.</summary>
    private const string StripeWebhookVariable = "STRIPE__WEBHOOKSECRET";

    /// <summary>The environment variable that carries a PayPal client id.</summary>
    private const string PayPalClientVariable = "PAYPAL__CLIENTID";

    /// <summary>The environment variable that carries a PayPal client secret.</summary>
    private const string PayPalSecretVariable = "PAYPAL__CLIENTSECRET";

    /// <summary>The environment variable that carries the PayPal webhook id.</summary>
    private const string PayPalWebhookVariable = "PAYPAL__WEBHOOKID";

    [Fact]
    public async Task Sandbox_StripeEnabledCreate_ShouldReachTheProvider()
    {
        // Validate: the script enables the provider only when both secrets are present.
        if (!HasSecrets(StripeKeyVariable, StripeWebhookVariable))
            Assert.Skip($"Set {StripeKeyVariable} and {StripeWebhookVariable} to run the Stripe sandbox check.");

        AssertProviderEnabled(PaymentProvider.Stripe);
        await AssertProviderAcceptedAsync(PaymentProvider.Stripe);
    }

    [Fact]
    public async Task Sandbox_PayPalEnabledCreate_ShouldReachTheProvider()
    {
        if (!HasSecrets(PayPalClientVariable, PayPalSecretVariable, PayPalWebhookVariable))
            Assert.Skip(
                $"Set {PayPalClientVariable}, {PayPalSecretVariable}, and {PayPalWebhookVariable} to run the PayPal sandbox check.");

        AssertProviderEnabled(PaymentProvider.PayPal);
        await AssertProviderAcceptedAsync(PaymentProvider.PayPal);
    }

    /// <summary>Creates a payment through the configured provider and asserts the resource.</summary>
    private async Task AssertProviderAcceptedAsync(PaymentProvider provider)
    {
        if (fixture.SkipReason is not null)
            Assert.Skip(fixture.SkipReason);

        using HttpClient client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(new
            {
                amount = new { amount = 1m, currency = "USD" },
                provider = provider.ToWire(),
                externalReference = $"sandbox-{Guid.NewGuid():N}",
                captureMode = "automatic"
            })
        };

        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using HttpResponseMessage response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().BeOneOf(
            [HttpStatusCode.Created, HttpStatusCode.OK],
            "the sandbox provider answered: {0}",
            body);
    }

    /// <summary>Reports whether every required environment variable is present.</summary>
    private static bool HasSecrets(params string[] variables)
        => variables.All(name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)));

    /// <summary>Fails loudly when the secret is set but the host did not register the provider.</summary>
    private void AssertProviderEnabled(PaymentProvider provider)
    {
        PaymentsOptions options = fixture.Factory.Services
            .GetRequiredService<IOptions<PaymentsOptions>>()
            .Value;

        bool enabled = provider switch
        {
            PaymentProvider.Stripe => options.Providers.Stripe.Enabled,
            PaymentProvider.PayPal => options.Providers.PayPal.Enabled,
            _ => options.Providers.Fake.Enabled
        };

        enabled.Should().BeTrue(
            $"the sandbox script must export Payments__Providers__{provider}.__Enabled before the host boots");
    }
}
