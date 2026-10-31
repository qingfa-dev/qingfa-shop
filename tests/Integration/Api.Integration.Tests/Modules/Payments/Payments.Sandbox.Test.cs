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

    /// <summary>The environment variable that carries a PayPal client secret.</summary>
    private const string PayPalSecretVariable = "PAYPAL__CLIENTSECRET";

    [Fact]
    public async Task Sandbox_StripeEnabledCreate_ShouldReachTheProvider()
    {
        if (!IsEnabled(StripeKeyVariable))
            Assert.Skip($"Set {StripeKeyVariable} to run the Stripe sandbox check.");

        await AssertProviderAcceptedAsync(PaymentProvider.Stripe);
    }

    [Fact]
    public async Task Sandbox_PayPalEnabledCreate_ShouldReachTheProvider()
    {
        if (!IsEnabled(PayPalSecretVariable))
            Assert.Skip($"Set {PayPalSecretVariable} to run the PayPal sandbox check.");

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

    /// <summary>Reports whether a provider is enabled and its secret is present.</summary>
    private bool IsEnabled(string variable)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
            return false;

        PaymentsOptions options = fixture.Factory.Services
            .GetRequiredService<IOptions<PaymentsOptions>>()
            .Value;

        return options.Providers.Stripe.Enabled || options.Providers.PayPal.Enabled;
    }
}
