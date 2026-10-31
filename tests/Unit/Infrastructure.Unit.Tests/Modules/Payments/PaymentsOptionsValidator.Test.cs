using AwesomeAssertions;

using Infrastructure.Modules.Payments;
using Infrastructure.Modules.Payments.Adapters.PayPal;

namespace Infrastructure.Unit.Tests.Modules.Payments;

public sealed class PaymentsOptionsValidatorTest
{
    [Fact]
    public void Validate_FakeInProduction_ShouldFail()
    {
        var options = new PaymentsOptions
        {
            DefaultProvider = "Fake",
            Providers = new PaymentProvidersOptions { Fake = new FakeProviderOptions { Enabled = true } }
        };

        var result = new PaymentsOptionsValidator("Production").Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_EnabledStripeWithoutSecrets_ShouldFail()
    {
        var options = new PaymentsOptions
        {
            DefaultProvider = "Fake",
            Providers = new PaymentProvidersOptions
            {
                Fake = new FakeProviderOptions { Enabled = true },
                Stripe = new StripeProviderOptions { Enabled = true, SecretKey = "", WebhookSecret = "" }
            }
        };

        var result = new PaymentsOptionsValidator("Development").Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_EnabledPayPalWithoutSecrets_ShouldFail()
    {
        var options = new PaymentsOptions
        {
            DefaultProvider = "Fake",
            Providers = new PaymentProvidersOptions
            {
                Fake = new FakeProviderOptions { Enabled = true },
                PayPal = new PayPalProviderOptions { Enabled = true }
            }
        };

        var result = new PaymentsOptionsValidator("Development").Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90000)]
    public void Validate_IntentLifetimeOutOfRange_ShouldFail(int lifetime)
    {
        PaymentsOptions options = Valid(lifetime: lifetime);

        new PaymentsOptionsValidator("Development").Validate(null, options).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_WebhookLimitOutOfRange_ShouldFail()
    {
        PaymentsOptions options = new()
        {
            DefaultProvider = "Fake",
            Limits = new PaymentLimitsOptions { WebhookMaxBytes = 10 },
            Providers = new PaymentProvidersOptions { Fake = new FakeProviderOptions { Enabled = true } }
        };

        new PaymentsOptionsValidator("Development").Validate(null, options).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_DisabledDefaultProvider_ShouldFail()
    {
        var options = new PaymentsOptions
        {
            DefaultProvider = "Stripe",
            Providers = new PaymentProvidersOptions { Fake = new FakeProviderOptions { Enabled = true } }
        };

        new PaymentsOptionsValidator("Development").Validate(null, options).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_DefaultConfiguration_ShouldSucceed()
    {
        PaymentsOptions options = Valid();

        ValidateOptionsResultShim.IsSuccess(
            new PaymentsOptionsValidator("Development").Validate(null, options)).Should().BeTrue();
    }

    /// <summary>Builds a valid configuration.</summary>
    private static PaymentsOptions Valid(int lifetime = 1800) => new()
    {
        DefaultProvider = "Fake",
        IntentLifetimeSeconds = lifetime,
        Providers = new PaymentProvidersOptions { Fake = new FakeProviderOptions { Enabled = true } }
    };

    /// <summary>Reads the success flag without depending on framework types.</summary>
    private static class ValidateOptionsResultShim
    {
        /// <summary>Reports whether the validation succeeded.</summary>
        internal static bool IsSuccess(Microsoft.Extensions.Options.ValidateOptionsResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return !result.Failed;
        }
    }
}
