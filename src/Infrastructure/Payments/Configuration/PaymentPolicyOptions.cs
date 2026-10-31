namespace Infrastructure.Payments.Configuration;

/// <summary>The per-currency payment policy bound from configuration.</summary>
public sealed class PaymentPolicyOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Payments:Policy";

    /// <summary>The maximum permitted amount for each supported currency.</summary>
    public Dictionary<string, decimal> MaximumAmountsByCurrency { get; set; } = [];

    /// <summary>How long a hosted-checkout intent stays payable.</summary>
    public TimeSpan HostedCheckoutIntentLifetime { get; set; }
        = TimeSpan.FromMinutes(30);

    /// <summary>Extra tolerance applied to provider-side expiry and late events.</summary>
    public TimeSpan ProviderExpiryGracePeriod { get; set; }
        = TimeSpan.FromMinutes(2);
}
