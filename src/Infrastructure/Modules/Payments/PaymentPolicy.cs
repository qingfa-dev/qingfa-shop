using Application.Modules.Payments.Ports;

using Microsoft.Extensions.Options;

namespace Infrastructure.Modules.Payments;

/// <summary>Exposes the configured payment limits to the application slices.</summary>
public sealed class PaymentPolicy : IPaymentPolicy
{
    /// <summary>Creates the policy from the bound configuration.</summary>
    /// <param name="options">The payments configuration.</param>
    public PaymentPolicy(IOptions<PaymentsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        PaymentsOptions value = options.Value;

        MaxAmount = value.Limits.MaxAmount;
        IntentLifetime = TimeSpan.FromSeconds(value.IntentLifetimeSeconds);
        ExpiryGrace = TimeSpan.FromSeconds(value.ExpiryGraceSeconds);
    }

    /// <inheritdoc />
    public decimal MaxAmount { get; }

    /// <inheritdoc />
    public TimeSpan IntentLifetime { get; }

    /// <inheritdoc />
    public TimeSpan ExpiryGrace { get; }
}
