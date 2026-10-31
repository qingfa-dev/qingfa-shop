namespace Infrastructure.Modules.Payments.Adapters.Stripe;

/// <summary>Stripe credentials.</summary>
public sealed class StripeOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Payments:Providers:Stripe";

    /// <summary>The secret API key.</summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>The webhook signing secret.</summary>
    public string WebhookSecret { get; init; } = string.Empty;

    /// <summary>How far a webhook timestamp may drift, in seconds.</summary>
    public long WebhookToleranceSeconds { get; init; } = 300;
}
