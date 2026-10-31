using Core.Modules.Payments;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Validates the payments configuration at startup.</summary>
public sealed class PaymentsOptionsValidator(string environment) : IValidateOptions<PaymentsOptions>
{
    /// <summary>The environment name that forbids the fake provider.</summary>
    private const string Production = "Production";

    /// <summary>The largest accepted intent lifetime.</summary>
    private const int MaxLifetimeSeconds = 86400;

    /// <summary>The largest accepted expiry grace.</summary>
    private const int MaxGraceSeconds = 3600;

    /// <summary>The smallest accepted webhook body limit.</summary>
    private const int MinWebhookBytes = 1024;

    /// <summary>The largest accepted webhook body limit.</summary>
    private const int MaxWebhookBytes = 1048576;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaymentsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.Limits.MaxAmount <= 0)
            failures.Add("Payments:Limits:MaxAmount must be greater than zero.");

        if (options.IntentLifetimeSeconds is < 1 or > MaxLifetimeSeconds)
            failures.Add("Payments:IntentLifetimeSeconds must be between 1 and 86400.");

        if (options.ExpiryGraceSeconds is < 0 or > MaxGraceSeconds)
            failures.Add("Payments:ExpiryGraceSeconds must be between 0 and 3600.");

        if (options.Limits.WebhookMaxBytes is < MinWebhookBytes or > MaxWebhookBytes)
            failures.Add("Payments:Limits:WebhookMaxBytes must be between 1024 and 1048576.");

        if (options.Providers.Fake.Enabled && environment == Production)
            failures.Add("Payments:Providers:Fake must not be enabled in Production.");

        if (options.Providers.Stripe.Enabled
            && (string.IsNullOrWhiteSpace(options.Providers.Stripe.SecretKey)
                || string.IsNullOrWhiteSpace(options.Providers.Stripe.WebhookSecret)))
            failures.Add("Payments:Providers:Stripe requires SecretKey and WebhookSecret.");

        if (options.Providers.PayPal.Enabled
            && (string.IsNullOrWhiteSpace(options.Providers.PayPal.ClientId)
                || string.IsNullOrWhiteSpace(options.Providers.PayPal.ClientSecret)
                || string.IsNullOrWhiteSpace(options.Providers.PayPal.WebhookId)))
            failures.Add("Payments:Providers:PayPal requires ClientId, ClientSecret, and WebhookId.");

        if (!Enum.IsDefined(options.Providers.PayPal.Environment))
            failures.Add("Payments:Providers:PayPal:Environment must be Sandbox or Live.");

        if (!IsEnabled(options, options.DefaultProvider))
            failures.Add($"Payments:DefaultProvider '{options.DefaultProvider}' is not enabled.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>Reports whether a provider is enabled.</summary>
    private static bool IsEnabled(PaymentsOptions options, string provider) => provider switch
    {
        nameof(PaymentProvider.Fake) => options.Providers.Fake.Enabled,
        nameof(PaymentProvider.Stripe) => options.Providers.Stripe.Enabled,
        nameof(PaymentProvider.PayPal) => options.Providers.PayPal.Enabled,
        _ => false
    };
}
