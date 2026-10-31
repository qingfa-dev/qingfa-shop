using Application.Modules.Payments.Ports;

using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Exposes the configured payment policy to the application slices.</summary>
public sealed class PaymentPolicy : IPaymentPolicy
{
    /// <summary>Creates the policy from the bound configuration.</summary>
    /// <param name="options">The policy configuration.</param>
    public PaymentPolicy(IOptions<PaymentPolicyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        PaymentPolicyOptions value = options.Value;

        MaximumAmountsByCurrency = new Dictionary<string, decimal>(
            value.MaximumAmountsByCurrency,
            StringComparer.OrdinalIgnoreCase);

        HostedCheckoutIntentLifetime = value.HostedCheckoutIntentLifetime;
        ProviderExpiryGracePeriod = value.ProviderExpiryGracePeriod;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, decimal> MaximumAmountsByCurrency { get; }

    /// <inheritdoc />
    public TimeSpan HostedCheckoutIntentLifetime { get; }

    /// <inheritdoc />
    public TimeSpan ProviderExpiryGracePeriod { get; }
}
