using Infrastructure.Modules.Payments.Adapters.PayPal;

namespace Infrastructure.Modules.Payments;

/// <summary>Payments configuration.</summary>
public sealed class PaymentsOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Payments";

    /// <summary>The provider used when a caller omits one.</summary>
    public string DefaultProvider { get; init; } = nameof(Core.Modules.Payments.PaymentProvider.Fake);

    /// <summary>Intent lifetime in seconds.</summary>
    public int IntentLifetimeSeconds { get; init; } = 1800;

    /// <summary>Grace period before expiry is enforced.</summary>
    public int ExpiryGraceSeconds { get; init; } = 300;

    /// <summary>Amount and body limits.</summary>
    public PaymentLimitsOptions Limits { get; init; } = new();

    /// <summary>Per-provider settings.</summary>
    public PaymentProvidersOptions Providers { get; init; } = new();
}

/// <summary>Amount and body limits.</summary>
public sealed class PaymentLimitsOptions
{
    /// <summary>The largest accepted amount.</summary>
    public decimal MaxAmount { get; init; } = Core.Modules.Payments.Intents.PaymentConstant.Defaults.MaxAmount;

    /// <summary>The largest accepted webhook body.</summary>
    public int WebhookMaxBytes { get; init; } = 65536;
}

/// <summary>Per-provider settings.</summary>
public sealed class PaymentProvidersOptions
{
    /// <summary>The fake provider.</summary>
    public FakeProviderOptions Fake { get; init; } = new();

    /// <summary>Stripe.</summary>
    public StripeProviderOptions Stripe { get; init; } = new();

    /// <summary>PayPal.</summary>
    public PayPalProviderOptions PayPal { get; init; } = new();
}

/// <summary>Fake provider settings.</summary>
public sealed class FakeProviderOptions
{
    /// <summary>Whether the fake provider is registered.</summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>Stripe settings.</summary>
public sealed class StripeProviderOptions
{
    /// <summary>Whether Stripe is registered.</summary>
    public bool Enabled { get; init; }

    /// <summary>The secret key.</summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>The publishable key.</summary>
    public string PublishableKey { get; init; } = string.Empty;

    /// <summary>The webhook secret.</summary>
    public string WebhookSecret { get; init; } = string.Empty;
}

/// <summary>PayPal settings.</summary>
public sealed class PayPalProviderOptions
{
    /// <summary>Whether PayPal is registered.</summary>
    public bool Enabled { get; init; }

    /// <summary>The REST client id.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>The REST client secret.</summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>The webhook id.</summary>
    public string WebhookId { get; init; } = string.Empty;

    /// <summary>Sandbox or Live.</summary>
    public PayPalEnvironment Environment { get; init; } = PayPalEnvironment.Sandbox;
}
