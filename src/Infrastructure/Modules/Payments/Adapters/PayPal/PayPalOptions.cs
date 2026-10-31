namespace Infrastructure.Modules.Payments.Adapters.PayPal;

/// <summary>PayPal credentials and environment.</summary>
public sealed class PayPalOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Payments:Providers:PayPal";

    /// <summary>The REST client id.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>The REST client secret.</summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>The configured webhook id.</summary>
    public string WebhookId { get; init; } = string.Empty;

    /// <summary>Sandbox or Live.</summary>
    public PayPalEnvironment Environment { get; init; } = PayPalEnvironment.Sandbox;

    /// <summary>The REST base address.</summary>
    public string BaseAddress => Environment is PayPalEnvironment.Live
        ? "https://api-m.paypal.com/"
        : "https://api-m.sandbox.paypal.com/";
}
